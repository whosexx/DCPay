using DarkV.Extension.Bases;
using DarkV.Extension.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCPay.Core.RPC
{
    public class RPCPacket
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

        [JsonProperty("method", NullValueHandling = NullValueHandling.Ignore)]
        public string Method { get; set; }

        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public object Params {get;set;}

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object Result { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public RPCError Error { get; set; }

        public RPCPacket() { this.Id = Guid.NewGuid().ToString("N"); }

        public RPCPacket(string id) => this.Id = id;

        public RPCPacket(string method, object param)
        {
            this.Id = Guid.NewGuid().ToString("N");
            this.Method = method;
            this.Params = param;
        }

        public RPCPacket(string id, string method, object param)
        {
            this.Id = id;
            this.Method = method;
            this.Params = param;
        }

        //public static implicit operator RPCPacket((string method, object[] @params) rpc)
        //    => new RPCRequest(rpc.method, rpc.@params);

        //public static implicit operator RPCRequest(string method)
        //    => new RPCRequest(method);

        public override string ToString() => Convert.ToBase64String(this.ToJSON().ToBytes());

        public static implicit operator RPCPacket(string base64)
        {
            var bs = Convert.FromBase64String(base64);
            if (bs == null
                || bs.Length <= 0)
                throw new FormatException("不是正确的base64编码。");

            var js = Encoding.UTF8.GetString(bs);
            if (string.IsNullOrWhiteSpace(js))
                throw new Exception("空白字符串，无法解析为JSON");

            return js.ToObject<RPCPacket>();
        }
    }
}
