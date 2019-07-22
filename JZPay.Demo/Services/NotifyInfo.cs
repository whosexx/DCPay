using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace JZPay.Demo.Services
{
    public class NotifyInfo
    {
        public static readonly DateTime UNIX = new DateTime(1970, 1, 1, 8, 0, 0);

        [JsonProperty("orderId")]
        public string orderId { get; set; }

        /// <summary>
        /// 对接平台订单Id
        /// </summary>
        [JsonProperty("jOrderId")]
        public string jOrderId { get; set; }

        /// <summary>
        /// 订单类型（目前仅充值)
        /// </summary>
        [JsonProperty("orderType")]
        public int orderType { get; set; }

        /// <summary>
        /// 支付货币，目前仅支持CNY
        /// </summary>
        [JsonProperty("currency")]
        public string currency { get; set; }

        /// <summary>
        /// 订单金额
        /// </summary>
        [JsonProperty("amount")]
        public decimal amount { get; set; }

        [JsonProperty("actualAmount")]
        public decimal actualAmount { get; set; }

        /// <summary>
        /// 对接平台订单扩展信息，返回时原样返回
        /// </summary>
        [JsonProperty("fee")]
        public decimal fee { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("payWay")]
        public PayWay payWay { get; set; }

        /// <summary>
        /// 对接平台订单扩展信息，返回时原样返回
        /// </summary>
        [JsonProperty("jExtra")]
        public string jExtra { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        [JsonProperty("status")]
        public int status { get; set; }

        [JsonProperty("payTime")]
        public long payTime { get; set; }

        [JsonProperty("notifyUrl")]
        public string notifyUrl { get; set; }

        public DateTime GetPayTime() => UNIX.AddSeconds(this.payTime);
    }
}
