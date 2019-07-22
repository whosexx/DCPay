using DarkV.Extension.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace JZPay.Demo.Services
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
        public string jUserId { get; set; }

        /// <summary>
        /// 对接平台用户IP:用于安全策略
        /// </summary>
        [JsonProperty("jUserIp")]
        public string jUserIp { get; set; }

        /// <summary>
        /// 对接平台订单Id
        /// </summary>
        [JsonProperty("jOrderId")]
        public string jOrderId { get; set; }

        /// <summary>
        /// 对接平台订单扩展信息，返回时原样返回
        /// </summary>
        [JsonProperty("jExtra")]
        public string jExtra { get; set; }

        /// <summary>
        /// 订单类型（目前仅充值)
        /// </summary>
        [JsonProperty("orderType")]
        public int orderType { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("payWay")]
        public PayWay payWay { get; set; }

        /// <summary>
        /// 订单金额
        /// </summary>
        [JsonProperty("amount")]
        public decimal amount { get; set; }

        /// <summary>
        /// 支付货币，目前仅支持CNY
        /// </summary>
        [JsonProperty("currency")]
        public string currency { get; set; }

        /// <summary>
        /// 回调通知
        /// </summary>
        [JsonProperty("notifyUrl")]
        public string notifyUrl { get; set; }
    }

    public enum PayWay
    {
        AliPay,
        WechatPay
    }

    public class OrderResponseInfo
    {
        [JsonProperty("orderId")]
        public string orderId { get; set; }

        [JsonProperty("orderType")]
        public int orderType { get; set; }

        [JsonProperty("paymentUrl")]
        public string paymentUrl { get; set; }

        public override string ToString() => this.ToJSON();

    }
}
