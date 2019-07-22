using DarkV.Extension.Bases;
using DarkV.Extension.Json;
using Org.BouncyCastle.Crypto.Parameters;
using JZPay.Core.Channels;
using JZPay.Core.RPC;
using JZPay.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DarkV.Extension.Crypto;
using DarkV.Extension.Collections;
using System.Threading.Tasks;
using JZPay.Core.Configurations;
using JZPay.Core.Channels.Pools;

namespace JZPay.Core
{
    public class SecureProtocolBase : InitializedBase<Dictionary<string, string>>, ISecureProtocol
    {
        public virtual event Func<ISecureProtocol, ReceiveServerMessageEventArgs, ResultInfo> ReceiveServerMessageEvent;

        public virtual event Action<ISecureProtocol, ResultInfo> ChannelErrorEvent;

        public string Platform { get; set; }

        public SecureKeyInfo SecureKey { get; set; }

        public DateTime Last { get; protected set; } = DateTime.Now;

        protected IAsyncChannel<Packet> Channel { get; set; }
        
        public int SendTimeout => this.Params.GetValue(nameof(SendTimeout), 10 * 1000);

        public int RecvTimeout => this.Params.GetValue(nameof(RecvTimeout), 10 * 1000);

        public bool UseConnectionPools => this.Params.GetValue(nameof(UseConnectionPools), false);

        public int MaxConnectionPoolCount => this.Params.GetValue(nameof(MaxConnectionPoolCount), 10);

        private ClearKeyInfo ClearKey { get; set; }

        public SecureProtocolBase() { }

        public SecureProtocolBase(string platform)
        {
            this.Platform = platform;
        }

        public SecureProtocolBase(string platform, SecureKeyInfo secure)
        {
            this.Platform = platform;
            this.SecureKey = secure;
        }

        protected override void InitializeInternal(Dictionary<string, string> arg)
        {
            base.InitializeInternal(arg);
            if (this.SecureKey != null)
            {
                if (this.Platform != this.SecureKey.Platform)
                    throw new ArgumentException("参数值不一致，无法使用。");

                if (!this.SecureKey.Verify())
                    throw new ArgumentException($"参数[{nameof(this.SecureKey)}]校验失败。");
                else
                    this.ClearKey = (ClearKeyInfo)this.SecureKey;
            }

            if (this.ClearKey == null)
                this.Channel = new AsyncChannel<Packet>(DefaultConfiguration.Default.DefaultWSSUrl);
            else
            {
                if (!this.UseConnectionPools)
                    this.Channel = new SecureAsyncChannel<Packet>(DefaultConfiguration.Default.DefaultWSSUrl,
                        this.ClearKey.ClearClientPrivateKey,
                        this.ClearKey.ClearServerPublicKey);
                else
                    this.Channel = new ChannelPool<Packet>(this.MaxConnectionPoolCount, () =>
                    {
                        var s = new SecureAsyncChannel<Packet>(DefaultConfiguration.Default.DefaultWSSUrl,
                            this.ClearKey.ClearClientPrivateKey,
                            this.ClearKey.ClearServerPublicKey);
                        s.Initialize(this.Params);
                        return s;
                    });
            }

            this.Channel.ConnectedEvent += Channel_ConnectedEvent;
            this.Channel.DisConnectedEvent += Channel_DisConnectedEvent;
            this.Channel.ReceivePacketEvent += Channel_RevicePacketEvent;
            this.Channel.ReceiveUnknownDataEvent += Channel_ReceiveUnknownDataEvent;
            this.Channel.Initialize(this.Params);
        }

        private void Channel_ConnectedEvent(IAsyncChannel<Packet> obj)
        {
            //立即发送登陆信息
            if (this.ClearKey != null)
            {
                Logger.Debug("已经交换过密钥，请求登陆。");
                this.Login(obj);
            }
        }

        private void Channel_RevicePacketEvent(IAsyncChannel<Packet> obj, Packet pkg)
        {
            this.Last = DateTime.Now;
            var rpc = (RPCPacket)pkg.Message;
            if(!string.IsNullOrWhiteSpace(rpc.Method))
            {
                //服务端请求的消息
                var r = this.ReceiveServerMessageEvent?.Invoke(this, new ReceiveServerMessageEventArgs
                {
                    Method = rpc.Method,
                    Params = rpc.Params.ToString(),
                    Data = rpc.Params.ToString().ToObject<NotifyInfo>()
                });

                if (r == null)
                    r = new ResultInfo { Code = ErrorCode.UnhandleMessage, Message = "外部没有处理此消息。" };

                RPCPacket p = new RPCPacket(rpc.Id);
                if (r.Code != 0)
                {
                    p.Error = new RPCError
                    {
                        Code = r.Code,
                        Message = r.Message,
                        Data = r.Data
                    };
                }
                else
                    p.Result = r.Data;

                try { this.Channel.Send(new Packet(p.ToString(), this.Platform)); }
                catch (Exception exf) { Logger.Error("回复服务器的时候出错了：" + exf.ToString()); }
                return;
            }
            
            //区分消息属于谁。
            if(!this.Semaphores.TryGetValue(rpc.Id, out var v))
            {
                Logger.Warn($"孤立的消息：[{rpc.Id}]，丢弃。");
                return;
            }

            v.Response = rpc.Result;
            v.Error = rpc.Error;
            v.ResponseTime = DateTime.Now;
            v.Release();
        }

        private void Channel_DisConnectedEvent(IAsyncChannel<Packet> obj)
        {
            Logger.Error($"通道[{obj.Name}]已经断开连接。");
            this.ChannelErrorEvent?.Invoke(this, new ResultInfo
            {
                Code = ErrorCode.ChannelError,
                Message = "通道断开链接。",
                Data = obj
            });
        }

        private void Channel_ReceiveUnknownDataEvent(IAsyncChannel<Packet> arg1, byte[] arg2)
        {
            this.ChannelErrorEvent?.Invoke(this, new ResultInfo
            {
                Code = ErrorCode.RecvUnknownData,
                Message = "通道数据无法解析。",
                Data = arg2
            });
        }
        #region sync
        protected readonly object sLocker = new object();
        protected Dictionary<string, SyncInfo>
            Semaphores = new Dictionary<string, SyncInfo>(StringComparer.InvariantCultureIgnoreCase);

        protected sealed class SyncInfo : DisposableBase
        {
            public string Id { get; set; }

            private SemaphoreSlim Slim { get; set; }

            public object Request { get; set; }

            public object Response { get; set; }

            public RPCError Error { get; set; }

            public DateTime RequestTime { get; set; }

            public DateTime ResponseTime { get; set; }

            public SyncInfo(string id, object r)
            {
                this.Id = id;
                this.Slim = new SemaphoreSlim(0, 1);
                this.Request = r;
                this.RequestTime = DateTime.Now;
                this.ResponseTime = DateTime.MinValue;
            }

            public bool Wait(int ms = -1) => this.Slim.Wait(ms);

            public void Release() => this.Slim.Release();

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if(disposing)
                {
                    if(this.Slim != null)
                    {
                        this.Slim.Dispose();
                        this.Slim = null;
                    }
                }
            }
        }


        protected void AddSync(SyncInfo sinfo)
        {
            lock (this.sLocker)
            {
                if (this.Semaphores.ContainsKey(sinfo.Id))
                    throw new Exception($"已经存在的Key[{sinfo.Id}]，无法再次添加。");

                this.Semaphores[sinfo.Id] = sinfo;
            }
        }

        protected void RemoveSync(string id)
        {
            lock (this.sLocker)
                this.Semaphores.Remove(id);
        }

        protected virtual ResultInfo<RET> Invoke<RET>(RPCPacket rpc, IAsyncChannel<Packet> chan)
        {
            if (this.ClearKey == null
                && rpc.Method != "exchangePublicKey")
                throw new NotSupportedException("尚未与服务端交换密钥，无法使用,请先调用接口【ExchangePublicKey】");

            if (chan == null)
                chan = this.Channel;

            SyncInfo sync = new SyncInfo(rpc.Id, rpc.Params);
            try
            {
                this.AddSync(sync);
                chan.Send(new Packet(rpc.ToString(), this.Platform));
                this.Last = DateTime.Now;
                if(!sync.Wait(this.RecvTimeout))
                {
                    sync.Error = new RPCError
                    {
                        Code = ErrorCode.ChannelError,
                        Message = "recv msg timeout",
                    };
                }

                if (sync.Error != null
                    && sync.Error.Code != 0)
                {
                    Logger.Error($"请求返回错误：[{sync.Error.Code}({sync.Error.Message})]");
                    return sync.Error;
                }

                if (sync.Response == null)
                    return ResultInfo<RET>.OK;

                return new ResultInfo<RET>
                {
                    Code = 0,
                    Message = "ok",
                    Data = sync.Response.ToString().ToObject<RET>()
                };
            }
            catch(Exception ex) { return new ResultInfo<RET> { Code = ErrorCode.UnknownError, Message = ex.Message }; }
            finally
            {
                this.RemoveSync(sync.Id);
                sync.Dispose();
            }
        }
        #endregion

        #region API
        public virtual ResultInfo<SecureKeyInfo> ExchangePublicKey(string authcode)
        {
            var prikey = Utils.GetRandomPrivateKey();
            var ed_puk = prikey.GetEd25519PublicKey();
            var curve = prikey.GetCurve25519PublicKey();

            var aes_key = prikey.GetShareKey(DefaultConfiguration.Default.DefaultPublicKey).GetRange(0, 16);
            //Logger.Debug($"AES Key: [{aes_key.ToHex()}][{DefaultConfiguration.Default.DefaultPublicKey.ToHex()}]");
            //TODO:加密p，然后传输
            var pkhex = curve.ToHex();
            //Logger.Debug($"Pri: {prikey.ToHex()}, ED: {ed_puk.ToHex()}，CURVE：{curve.ToHex()}");
            var encrypt = (pkhex+ "_" + ed_puk.ToHex() + "_" + authcode).ToBytes().AESEncrypt(aes_key).ToHex();
            var r =  this.Invoke<CipherInfo>(new RPCPacket("exchangePublicKey", new CipherInfo(encrypt, pkhex)), this.Channel);
            if (r.Code != 0)
                return new ResultInfo<SecureKeyInfo> { Code = r.Code, Message = r.Message };

            var pk = r.Data.Cipher.ToBinary().AESDecrypt(aes_key);
            var clear = new ClearKeyInfo()
            {
                ClearServerPublicKey = pk,
                ClearClientPrivateKey = prikey,
            };

            //TODO:密文公钥,解密以后在加密保存密钥信息，断开连接
            var s = ((SecureKeyInfo)clear);
            s.AuthCode = authcode;
            s.Platform = this.Platform;
            s.CheckSum = s.GetCheckSum();
            return new ResultInfo<SecureKeyInfo> { Code = 0, Message = "ok", Data = s };
        }

        protected virtual bool Login(IAsyncChannel<Packet> chan)
        {
            RPCPacket rpc = new RPCPacket("login", new { platform_id = this.Platform });
            var r = this.Invoke<object>(rpc, chan);
            if (r.Code == 0)
                return true;

            return false;
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(disposing)
            {
                if(this.Channel != null)
                {
                    this.Channel.Dispose();
                    this.Channel = null;
                }
            }
        }

        private class ClearKeyInfo
        {
            private const string Seed = "{8D38D3CB-A691-4B7B-96B7-F3549842B80C}";

            internal protected byte[] ClearServerPublicKey { get; set; }
            
            internal protected byte[] ClearClientPrivateKey { get; set; }

            public static explicit operator ClearKeyInfo(SecureKeyInfo secure)
            {
                var k = Seed.ToBytes().Sha256();
                var aes = new byte[16];
                Array.Copy(k, 8, aes, 0, aes.Length);
                return new ClearKeyInfo
                {
                    ClearClientPrivateKey = secure.ClientPrivateKey?.ToBinary().AESDecrypt(aes),
                    ClearServerPublicKey = secure.ServerPublicKey?.ToBinary().AESDecrypt(aes)
                };
            }

            public static explicit operator SecureKeyInfo(ClearKeyInfo clear)
            {
                var k = Seed.ToBytes().Sha256();
                var aes = new byte[16];
                Array.Copy(k, 8, aes, 0, aes.Length);
                
                return new SecureKeyInfo
                {
                    ClientPrivateKey = clear.ClearClientPrivateKey?.AESEncrypt(aes).ToHex(),
                    ServerPublicKey = clear.ClearServerPublicKey?.AESEncrypt(aes).ToHex()
                };
            }
        }
    }
}
