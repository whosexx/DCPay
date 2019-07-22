//using DarkV.Extension.Bases;
//using DarkV.Extension.Json;
//using Org.BouncyCastle.Crypto.Parameters;
//using DCPay.Core.Channels;
//using DCPay.Core.RPC;
//using DCPay.Core.Services;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Threading;
//using DarkV.Extension.Crypto;
//using DarkV.Extension.Collections;
//using System.Threading.Tasks;
//using DCPay.Core.Configurations;
//using DCPay.Core.Channels.Pools;

//namespace DCPay.Core
//{
//    public class SyncLockerManage
//    {
//        protected readonly object sLocker = new object();
//        protected Dictionary<string, SyncInfo> Semaphores = new Dictionary<string, SyncInfo>();

//        //private void Channel_RevicePacketEvent(IAsyncChannel<Packet> obj, Packet pkg)
//        //{
//        //    this.Last = DateTime.Now;
//        //    var rpc = (RPCPacket)pkg.Message;
//        //    if(!string.IsNullOrWhiteSpace(rpc.Method))
//        //    {
//        //        //服务端请求的消息
//        //        var r = this.ReceiveServerMessageEvent?.Invoke(this, new ReceiveServerMessageEventArgs
//        //        {
//        //            Method = rpc.Method,
//        //            Params = rpc.Params.ToString(),
//        //            Data = rpc.Params.ToString().ToObject<NotifyInfo>()
//        //        });

//        //        RPCPacket p = new RPCPacket(rpc.Id);
//        //        if (r.Code != 0)
//        //            p.Error = new RPCError
//        //            {
//        //                Code = r.Code,
//        //                Message = r.Message,
//        //                Data = r.Data
//        //            };
//        //        else
//        //            p.Result = r.Data;

//        //        this.Channel.Send(new Packet(p.ToString(), this.Platform));
//        //        return;
//        //    }
            
//        //    //区分消息属于谁。
//        //    if(!this.Semaphores.TryGetValue(rpc.Id, out var v))
//        //    {
//        //        Logger.Warn($"孤立的消息：[{rpc.Id}]，丢弃。");
//        //        return;
//        //    }

//        //    v.Response = rpc.Result;
//        //    v.Error = rpc.Error;
//        //    v.ResponseTime = DateTime.Now;
//        //    v.Release();
//        //}

        

        

//        protected void AddSync(SyncInfo sinfo)
//        {
//            lock (this.sLocker)
//            {
//                if (this.Semaphores.ContainsKey(sinfo.Id))
//                    throw new Exception($"已经存在的Key[{sinfo.Id}]，无法再次添加。");

//                this.Semaphores[sinfo.Id] = sinfo;
//            }
//        }

//        protected void RemoveSync(string id)
//        {
//            lock (this.sLocker)
//                this.Semaphores.Remove(id);
//        }

//        protected virtual ResultInfo<RET> Invoke<RET>(RPCPacket rpc, IAsyncChannel<Packet> chan)
//        {
//            if (this.ClearKey == null
//                && rpc.Method != "exchangePublicKey")
//                throw new NotSupportedException("尚未与服务端交换密钥，无法使用,请先调用接口【ExchangePublicKey】");

//            if (chan == null)
//                chan = this.Channel;

//            SyncInfo sync = new SyncInfo(rpc.Id, rpc.Params);
//            try
//            {
//                this.AddSync(sync);
//                chan.Send(new Packet(rpc.ToString(), this.Platform));
//                this.Last = DateTime.Now;
//                if (!sync.Wait(this.RecvTimeout))
//                {
//                    sync.Error = new RPCError
//                    {
//                        Code = -1001,
//                        Message = "recv msg timeout",
//                    };
//                }

//                if (sync.Error != null
//                    && sync.Error.Code != 0)
//                {
//                    Logger.Error($"请求返回错误：[{sync.Error.Code}({sync.Error.Message})]");
//                    return sync.Error;
//                }

//                if (sync.Response == null)
//                    return ResultInfo<RET>.OK;

//                return new ResultInfo<RET>
//                {
//                    Code = 0,
//                    Message = "ok",
//                    Data = sync.Response.ToString().ToObject<RET>()
//                };
//            }
//            finally
//            {
//                this.RemoveSync(sync.Id);
//                sync.Dispose();
//            }
//        }
//    }

//    protected sealed class SyncInfo : DisposableBase
//    {
//        public string Id { get; set; }

//        private SemaphoreSlim Slim { get; set; }

//        public object Request { get; set; }

//        public object Response { get; set; }

//        public RPCError Error { get; set; }

//        public DateTime RequestTime { get; set; }

//        public DateTime ResponseTime { get; set; }

//        public SyncInfo(string id, object r)
//        {
//            this.Id = id;
//            this.Slim = new SemaphoreSlim(0, 1);
//            this.Request = r;
//            this.RequestTime = DateTime.Now;
//            this.ResponseTime = DateTime.MinValue;
//        }

//        public bool Wait(int ms = -1) => this.Slim.Wait(ms);

//        public void Release() => this.Slim.Release();

//        protected override void Dispose(bool disposing)
//        {
//            base.Dispose(disposing);
//            if (disposing)
//            {
//                if (this.Slim != null)
//                {
//                    this.Slim.Dispose();
//                    this.Slim = null;
//                }
//            }
//        }
//    }

//}
