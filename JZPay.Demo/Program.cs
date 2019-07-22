using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using JZPay.Demo.Services;
using System.Text;
using DarkV.Extension.Bases;
using Newtonsoft.Json.Linq;
using DarkV.Extension.Json;

namespace JZPay.Demo
{
    public static class HttpExtensions
    {
        public static async Task<T> Post<T>(this string url, IEnumerable<KeyValuePair<string, string>> datas)
        {
            var js = await url.Post(datas);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(js);
        }

        public static async Task<string> Post(this string url, IEnumerable<KeyValuePair<string, string>> datas)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true,

            };

            using (var web = new HttpClient(handler))
            {
                web.Timeout = new TimeSpan(0, 0, 30);
                using (FormUrlEncodedContent form = new FormUrlEncodedContent(datas))
                {
                    Console.WriteLine($"请求数据：" + await form.ReadAsStringAsync());
                    using (var rep = await web.PostAsync(url, form))
                    {
                        rep.EnsureSuccessStatusCode();
                        var ret = await rep.Content.ReadAsStringAsync();
                        Console.WriteLine($"返回数据：" + ret);
                        return ret;
                    }
                }
            }
        }
    }

    public class SignatureInfo
    {
        public static readonly DateTime Unix = new DateTime(1970, 1, 1, 0, 0, 0);

        public string merchantId { get; set; } = "4493ef72abc214347143fb52d7258aa3";

        public string timestamp { get; set; } = Convert.ToInt64((DateTime.UtcNow - Unix).TotalMilliseconds).ToString();

        public string signatureMethod { get; set; } = "HmacSHA256";

        public string signatureVersion { get; set; } = "1";

        public string signature { get; set; }

        public SignatureInfo() { }

        public SignatureInfo(string merchantId) => this.merchantId = merchantId;

        public List<string> GetPairs()
        {
            List<string> ret = new List<string>
            {
                $"{nameof(this.merchantId)}={this.merchantId}",
                $"{nameof(this.timestamp)}={this.timestamp}",
                $"{nameof(this.signatureMethod)}={this.signatureMethod}",
                $"{nameof(this.signatureVersion)}={this.signatureVersion}"
            };

            return ret;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var pairs = GetPairs();
            pairs.Add($"{nameof(this.signature)}={this.signature}");

            return string.Join('&', pairs);;
        }
    }

    public class Program
    {

        public const string DCPayUrl = "https://api.jzpay.vip/jzpay_exapi/v1";
        public const string MerchantId = "4493ef72abc214347143fb52d7258aa3";

        public static ResultInfo<T> Call<T>(string url, List<KeyValuePair<string, string>> forms)
        {
            SignatureInfo signature = new SignatureInfo(MerchantId);
            var ret = signature.GetPairs();
            foreach (var p in forms)
                ret.Add(p.Key + "=" + p.Value);

            ret = ret.OrderBy(m => m).ToList();
            signature.signature = string.Join('&', ret).ToBytes().HMACSHA256().ToHex().ToUpper();

            var js = (url + signature.ToString()).Post(forms).Result;
            var r = js.ToObject<ResultInfo<T>>();
            if (r.code != 0)
            {
                Console.WriteLine($"调用失败：" + r.message);
                return null;
            }
             
            Console.WriteLine($"调用成功：" + js);
            var verify = Verify(r, (JObject)JObject.Parse(js)["data"], MerchantId);
            Console.WriteLine("验证签名Verify: " + verify);
            if (!verify)
                return null;

            return r;
        }

        public static bool Verify<T>(ResultInfo<T> token, JObject obj, string merchantId)
        {
            if (token.code != 0)
                return false;

            var d = token.GetPairs(merchantId);
            var properties = token.data.GetType().GetProperties();
            foreach (var kv in obj)
            {
                var p = properties.FirstOrDefault(m => m.Name == kv.Key);
                if (p == null)
                    throw new Exception($"找不到属性[{kv.Key}]");

                d.Add(kv.Key + "=" + p.GetValue(token.data));
            }

            d = d.OrderBy(m => m).ToList();
            string sig = string.Join('&', d);
            Console.WriteLine("RET: " + sig);
            return string.Compare(token.signature, sig.ToBytes().HMACSHA256().ToHex().ToUpper(), true) == 0;
        }

        public static OrderResponseInfo CreateOrder()
        {
            string jorderid = Guid.NewGuid().ToString("N");
            Console.WriteLine(jorderid);

            string method = "/order/createOrder?";
            List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("jUserId", "987"),
                new KeyValuePair<string, string>("jUserIp", "10.1.1.175"),
                new KeyValuePair<string, string>("jOrderId", jorderid),
                new KeyValuePair<string, string>("orderType", ((int)OrderType.充值).ToString()),
                new KeyValuePair<string, string>("payWay", PayWay.WechatPay.ToString()),
                new KeyValuePair<string, string>("amount", "500.00"),
                new KeyValuePair<string, string>("currency", "CNY"),
                new KeyValuePair<string, string>("jExtra", "123"),
                new KeyValuePair<string, string>("notifyUrl", "https://api.example.com/notify")
            };

            var r = Call<OrderResponseInfo>(DCPayUrl + method, pairs);
            if (r == null)
                return null;

            return r.data;
        }

        public static OrderInfo QueryOrder(string orderid)
        {
            string method = "/order/queryOrder?";
            List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("orderId", orderid),
            };

            var r = Call<OrderInfo>(DCPayUrl + method, pairs);
            if (r == null)
                return null;

            return r.data;
        }
        
        public static void Main(string[] args)
        {
            var order = CreateOrder();
            if(order == null)
            {
                Console.WriteLine("下单失败。");
                return; 
            }

            var query_order = QueryOrder(order.orderId);
            if(query_order == null)
            {
                Console.WriteLine("查询失败。");
                return;
            }
        }
    }
}
