using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JZPay.Core.Services;
using JZPay.Core.Configurations;

namespace JZPay.Core
{
    public static class DCPayHelper
    {
        public static event Func<ReceiveServerMessageEventArgs, ResultInfo> ReceiveMessageEvent;
        public static event Action<ResultInfo> ReceiveUnknownDataEvent;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly SecurePayHelper<DCPayClient> Helper;

        static DCPayHelper()
        {
            Helper = new SecurePayHelper<DCPayClient>(Client_ReceiveServerMessageEvent);
            Helper.ReceiveUnknownDataEvent += Helper_ReceiveUnknownDataEvent;
            //Helper.ReceiveMessageHandler += Client_ReceiveServerMessageEvent;
        }

        private static void Helper_ReceiveUnknownDataEvent(DCPayClient arg1, ResultInfo arg2)
            => ReceiveUnknownDataEvent?.Invoke(arg2);

        private static ResultInfo Client_ReceiveServerMessageEvent(ISecureProtocol arg1, ReceiveServerMessageEventArgs arg2)
        {
            if (ReceiveMessageEvent == null)
                return new ResultInfo { Code = ErrorCode.UnhandleMessage, Message = "客户端没有处理通知。" };

            return ReceiveMessageEvent.Invoke(arg2);
        }

        public static ResultInfo<OrderResponseInfo> CreateOrder(OrderRequestInfo request, string platform, string authcode)
        {
            var c = Helper.GetPayClient(platform, authcode);
            if (c == null)
                return new ResultInfo<OrderResponseInfo> { Code = ErrorCode.GetClientError, Message = "获取连接出错，可能是authcode无法使用，也可能已经达到最大连接数。" };

            return c.CreateOrder(request);
        }

        public static ResultInfo<OrderInfo> QueryOrder(string orderId, string platform, string authcode)
        {
            var c = Helper.GetPayClient(platform, authcode);
            if (c == null)
                return new ResultInfo<OrderInfo> { Code = ErrorCode.GetClientError, Message = "获取连接出错，可能是authcode无法使用，也可能已经达到最大连接数。" };

            return c.QueryOrder(orderId);
        }

        public static ResultInfo<OrderInfo> QueryOrderByJOrderId(string jOrderId, string platform, string authcode)
        {
            var c = Helper.GetPayClient(platform, authcode);
            if (c == null)
                return new ResultInfo<OrderInfo> { Code = ErrorCode.GetClientError, Message = "获取连接出错，可能是authcode无法使用，也可能已经达到最大连接数。" };

            return c.QueryOrderByJOrderId(jOrderId);
        }
    }
}
