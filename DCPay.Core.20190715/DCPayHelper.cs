using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCPay.Core.Services;
using DCPay.Core.Configurations;

namespace DCPay.Core
{
    public static class DCPayHelper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly object cLocker = new object();
        private static PayClient Client { get; set; }

        //public static bool IsAuthenticated => SecureKeyInfo.Default != null && SecureKeyInfo.Default.Verify();

        public static event Func<NotifyInfo, ResultInfo> ReceiveMessageEvent;

        static DCPayHelper()
        {
            if(SecureKeyInfo.Default != null
                && !SecureKeyInfo.Default.Verify())
            {
                Logger.Warn("校验出错，清除交换密钥。");
                SecureKeyInfo.Clear();
            }

            if(SecureKeyInfo.Default != null 
                && SecureKeyInfo.Default.AuthCode != DefaultConfiguration.Default.AuthCode)
            {
                Logger.Warn("AuthCode已经发生改变，清除交换密钥。");
                SecureKeyInfo.Clear();
            }

            if (SecureKeyInfo.Default == null)
            {
                lock (cLocker)
                {
                    if (!GetClient().ExchangePublicKey(DefaultConfiguration.Default.AuthCode))
                    {
                        Logger.Fatal("交换密钥失败，无法处理。");
                        Dispose();
                        Environment.Exit(0);
                        return;
                    }

                    Logger.Debug("交换密钥成功。");
                    Dispose();
                }
            }
        }

        private static void Dispose()
        {
            var c = Client;
            Client = null;
            c.Dispose();
        }

        private static PayClient GetClient()
        {
            if (Client != null)
                return Client;

            lock(cLocker)
            {
                if (Client != null)
                    return Client;
                
                Client = new PayClient(DefaultConfiguration.Default.Platform, SecureKeyInfo.Default);
                Client.ReceiveServerMessageEvent += Client_ReceiveServerMessageEvent;
                Client.ChannelErrorEvent += Client_ChannelErrorEvent;
                Client.Initialize();
            }

            return Client;
        }

        private static ResultInfo Client_ReceiveServerMessageEvent(IPay arg1, NotifyInfo arg2)
        {
            if (ReceiveMessageEvent == null)
                return new ResultInfo { Code = -1003, Message = "客户端没有处理通知。" };

            return ReceiveMessageEvent?.Invoke(arg2);
        }

        private static void Client_ChannelErrorEvent(IPay arg1, ResultInfo arg2)
        {
            arg1.Dispose();
            lock(cLocker)
            {
                if (arg1 == Client)
                {
                    Client = null;
                    Task.Factory.StartNew(() => GetClient());
                }
            }
        }
        
        public static ResultInfo<OrderResponseInfo> CreateOrder(OrderRequestInfo request)
            => GetClient().CreateOrder(request);

        public static ResultInfo<OrderInfo> QueryOrder(string orderId)
            => GetClient().QueryOrder(orderId);

        public static ResultInfo<OrderInfo> QueryOrderByJOrderId(string jOrderId)
            => GetClient().QueryOrderByJOrderId(jOrderId);
    }
}
