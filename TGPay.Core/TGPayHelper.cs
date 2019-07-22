using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JZPay.Core.Services;
using JZPay.Core.Configurations;
using JZPay.Core;

namespace TGPay.Core
{
    public class TGPayHelper
    {
        public static event Func<ReceiveServerMessageEventArgs, ResultInfo> ReceiveMessageEvent;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly SecurePayHelper<DCPayClient> Helper;
        static TGPayHelper()
        {
            Helper = new SecurePayHelper<DCPayClient>(Client_ReceiveServerMessageEvent);
            //Helper.ReceiveMessageHandler += Client_ReceiveServerMessageEvent;
        }

        private static ResultInfo Client_ReceiveServerMessageEvent(ISecureProtocol arg1, ReceiveServerMessageEventArgs arg2)
        {
            if (ReceiveMessageEvent == null)
                return new ResultInfo { Code = -1003, Message = "客户端没有处理通知。" };

            return ReceiveMessageEvent?.Invoke(arg2);
        }

        public static ResultInfo<OrderResponseInfo> CreateOrder(OrderRequestInfo request, string platform, string authcode)
            => Helper.GetPayClient(platform, authcode).CreateOrder(request);

        public static ResultInfo<OrderInfo> QueryOrder(string orderId, string platform, string authcode)
            => Helper.GetPayClient(platform, authcode).QueryOrder(orderId);

        public static ResultInfo<OrderInfo> QueryOrderByJOrderId(string jOrderId, string platform, string authcode)
            => Helper.GetPayClient(platform, authcode).QueryOrderByJOrderId(jOrderId);
    }
}
