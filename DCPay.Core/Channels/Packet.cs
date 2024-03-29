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

namespace JZPay.Core.Channels
{
    public interface IBitConverter
    {
        byte[] Serialize();

        void Unserialize(byte[] data);
    }

    public interface ISignature
    {
        string Signature { get; set; }

        byte[] GetSignatureData();
    }

    public class Packet : IBitConverter, ISignature
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

        public virtual byte[] Serialize() => this.ToBytes();

        public virtual void Unserialize(byte[] data)
        {
            var p = (Packet)Encoding.UTF8.GetString(data);
            this.Message = p.Message;
            this.TimeStamp = p.TimeStamp;
            this.PlatformId = p.PlatformId;
            this.Params = p.Params;
            this.Signature = p.Signature;
        }

        public byte[] GetSignatureData()
            => (this.Message + this.TimeStamp).ToBytes();

        public byte[] ToBytes() => this.ToJSON().ToBytes();

        public override string ToString()
        {
            return $"{this.PlatformId} -- {((RPC.RPCPacket)this.Message).ToJSON()}";
        }

        public static implicit operator Packet(string js) => js.ToObject<Packet>();
    }
}
