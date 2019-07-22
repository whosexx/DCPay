using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCPay.Core.Services
{
    public enum OrderType
    {
        充值 = 1,
        提现 = 2
    }

    public class OrderRequestInfo
    {
        /// <summary>
        /// 对接平台订单用户:用于安全策略
        /// </summary>
        [JsonProperty("jUserId")]
        public string JUserId { get; set; }

        /// <summary>
        /// 对接平台用户IP:用于安全策略
        /// </summary>
        [JsonProperty("jUserIp")]
        public string JUserIp { get; set; }

        /// <summary>
        /// 对接平台订单Id
        /// </summary>
        [JsonProperty("jOrderId")]
        public string JOrderId { get; set; }

        /// <summary>
        /// 对接平台订单扩展信息，返回时原样返回
        /// </summary>
        [JsonProperty("jExtra")]
        public string JExtra { get; set; }

        /// <summary>
        /// 订单类型（目前仅充值)
        /// </summary>
        [JsonProperty("orderType")]
        public OrderType OrderType { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("payWay")]
        public PayWay PayWay { get; set; }

        /// <summary>
        /// 订单金额
        /// </summary>
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 支付货币，目前仅支持CNY
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// 回调通知
        /// </summary>
        [JsonProperty("notifyUrl")]
        public string NotifyUrl { get; set; }
    }

    public enum PayWay
    {
        AliPay,
        WechatPay
    }

    public class OrderResponseInfo
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("orderType")]
        public OrderType OrderType { get; set; }

        [JsonProperty("paymentUrl")]
        public string PaymentUrl { get; set; }

    }
}
