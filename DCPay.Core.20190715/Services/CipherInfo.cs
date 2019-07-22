using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCPay.Core.Services
{
    public class CipherInfo
    {
        [JsonProperty("cipher")]
        public string Cipher { get; set; }

        [JsonProperty("sdkecckey")]
        public string PublicKey { get; set; }

        public CipherInfo() { }

        public CipherInfo(string cipher, string pk)
        {
            this.Cipher = cipher;
            this.PublicKey = pk;
        }

        public static implicit operator CipherInfo(string cp) => new CipherInfo{ Cipher = cp };
    }
}
