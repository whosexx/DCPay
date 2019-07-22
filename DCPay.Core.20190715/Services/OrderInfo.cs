using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCPay.Core.Services
{
    public class OrderInfo
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        /// <summary>
        /// 对接平台订单Id
        /// </summary>
        [JsonProperty("jOrderId")]
        public string JOrderId { get; set; }

        /// <summary>
        /// 订单类型（目前仅充值)
        /// </summary>
        [JsonProperty("orderType")]
        public OrderType OrderType { get; set; }

        /// <summary>
        /// 支付货币，目前仅支持CNY
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// 订单金额
        /// </summary>
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 对接平台订单扩展信息，返回时原样返回
        /// </summary>
        [JsonProperty("fee")]
        public decimal Fee { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("payWay")]
        public PayWay PayWay { get; set; }

        /// <summary>
        /// 对接平台订单扩展信息，返回时原样返回
        /// </summary>
        [JsonProperty("jExtra")]
        public string JExtra { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        [JsonProperty("status")]
        public OrderStatus Status { get; set; }
    }

    public enum OrderStatus
    {
        已创建 = 1,
        已支付 = 2,
        完成 = 3,
        取消 = 4
    }
}
