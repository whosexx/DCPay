using JZPay.Core.Configurations;
using JZPay.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JZPay.Core
{
    public sealed class SecureClientManage<T> : Dictionary<string, T> where T : class, ISecureProtocol, new()
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static int MaxConnections { get; set; } = 100;

        public static int MaxClientIdleTime { get; set; } = 60 * 60 * 1000;

        public static readonly SecureClientManage<T> Default = new SecureClientManage<T>();
        private readonly object pLocker = new object();
        
        public event Action<T, ResultInfo> ReceiveUnknownDataEvent;
        private SecureClientManage()
        {
            Task.Factory.StartNew(() => 
            {
                while (true)
                {
                    try
                    {
                        foreach(var c in  this.Values.ToList())
                        {
                            if(c.Last.AddMilliseconds(MaxClientIdleTime) <= DateTime.Now)
                            {
                                Logger.Warn($"删除闲置通道[{c.Platform}]");
                                this.RemoveConnection(c.Platform);
                            }
                        }
                    }
                    catch (Exception ex) { Logger.Error($"维护连接池的时候报错了：" + ex); }

                    System.Threading.Thread.Sleep(60 * 1000);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public T CreateConnection(string platform, string auth, Func<ISecureProtocol, ReceiveServerMessageEventArgs, ResultInfo> func)
        {
            if (this.Count >= MaxConnections)
            {
                Logger.Warn($"连接池已经达到设置的最大值[{MaxConnections}]，无法继续创建连接。");
                return null;
            }

            if (this.TryGetValue(platform, out var value))
            {
                Logger.Warn($"已经存在平台[{platform}]的链接，即将重新创建链接。");

                lock (this.pLocker)
                    this.Remove(platform);
                value.Dispose();
            }

            var kinfo = SecureKeyManage.Default[platform];
            //需要交换密钥。
            if (kinfo != null
                && (kinfo.AuthCode != auth
                    || !kinfo.Verify()))
            {
                Logger.Warn($"授权码[{kinfo.AuthCode}--{auth}]已发生变化或者密钥校验失败，清除已缓存密钥。");
                SecureKeyManage.Default.Clear(platform);
                kinfo = null;
            }

            if(kinfo == null)
            {
                kinfo = this.ExchangePublicKey(platform, auth);
                if (kinfo == null)
                    return null;

                SecureKeyManage.Default[platform] = kinfo;
            }

            T pay = new T
            {
                Platform = platform,
                SecureKey = kinfo
            };

            pay.ReceiveServerMessageEvent += func;
            pay.ChannelErrorEvent += Client_ChannelErrorEvent;
            pay.Initialize();

            lock (this.pLocker)
                this[platform] = pay;
            return pay;
        }

        private void Client_ChannelErrorEvent(ISecureProtocol arg1, ResultInfo arg2)
        {
            if (arg2.Code == ErrorCode.ChannelError)
            {
                lock (this.pLocker)
                    this.Remove(arg1.Platform);
                arg1.Dispose();
                return;
            }

            if(arg2.Code == ErrorCode.RecvUnknownData)
            {
                this.ReceiveUnknownDataEvent?.Invoke((T)arg1, arg2);
                return;
            }
        }

        public T GetConnection(string platform, string auth)
        {
            if (this.TryGetValue(platform, out var value))
                return value;
            
            return null;
        }

        public void RemoveConnection(string platform)
        {
            lock (this.pLocker)
            {
                if (this.TryGetValue(platform, out var value))
                {
                    this.Remove(platform);
                    value.Dispose();
                }
            }
        }

        private SecureKeyInfo ExchangePublicKey(string platform, string authcode)
        {
            using (T pay = new T())
            {
                pay.Platform = platform;
                pay.Initialize();
                var ret = pay.ExchangePublicKey(authcode);
                if (ret.Code != 0)
                {
                    Logger.Error($"平台[{platform}]交换密钥失败，错误消息：[{ret.Message}]");
                    return null;
                }

                return ret.Data;
            }

        }
    }
}
