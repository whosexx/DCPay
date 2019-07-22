using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using DarkV.Extension.Bases;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using DarkV.Extension.Json;

namespace DCPay.Core.Channels
{
    public class Packet
    {
        public const string Format = "yyyy-MM-dd HH:mm:ss";

        [JsonProperty("msg")]
        public string Message { get; set; }

        [JsonProperty("timestamp")]
        public string TimeStamp { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("platform_id")]
        public string PlatformId { get; set; }
        
        [JsonProperty("params", 
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Params { get; set; }

        public Packet()
        {
            this.TimeStamp = DateTime.UtcNow.ToString(Format);
        }
        
        public Packet(string msg, string platform)
        {
            this.Message = msg;
            this.TimeStamp = DateTime.UtcNow.ToString(Format);
            this.PlatformId = platform ?? throw new ArgumentException(nameof(platform));
        }

        public byte[] GetSignBytes()
            => (this.Message + this.TimeStamp).ToBytes();

        public byte[] ToBytes() => this.ToJSON().ToBytes();

        public static implicit operator Packet(string js) => js.ToObject<Packet>();
    }
}
