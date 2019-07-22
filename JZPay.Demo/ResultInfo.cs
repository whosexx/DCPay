using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JZPay.Demo
{
    public class ResultInfo : ResultInfo<object>
    {
        public static readonly new ResultInfo OK = new ResultInfo
        {
            code = 0,
            message = "ok",
            data = new object { }
        };
        
    }

    public class ResultInfo<T>
    {
        public static readonly ResultInfo<T> OK = new ResultInfo<T>
        {
            code = 0,
            message = "ok",
        };

        public int code { get; set; }

        public string message { get; set; }

        public T data { get; set; }

        public string signature { get; set; }

        public string signatureMethod { get; set; } = "HmacSHA256";

        public string signatureVersion { get; set; } = "1";
        
        public List<string> GetPairs(string merchantId)
        {
            return new List<string>()
            {
                $"{nameof(this.code)}={this.code}",
                $"{nameof(this.message)}={this.message}",
                $"{nameof(this.signatureMethod)}={this.signatureMethod}",
                $"{nameof(this.signatureVersion)}={this.signatureVersion}",
                $"{nameof(merchantId)}={merchantId}",
            };
        }
    }

}
