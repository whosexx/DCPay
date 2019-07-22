using DarkV.Extension.Bases;
using DarkV.Extension.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCPay.Core.Configurations
{
    public class DefaultConfiguration
    {
        public const string KeyPath = "default.json";

        private static readonly object dLocker = new object();
        public static DefaultConfiguration Default = Load();

        public string DefaultWSSUrl { get; set; }

        public string DefaultPublicKeyString { get; set; }

        //public string Platform { get; set; }

        //public string AuthCode { get; set; }

        [JsonIgnore]
        public byte[] DefaultPublicKey { get; set; }

        public static DefaultConfiguration Load()
        {
            if (!System.IO.File.Exists(KeyPath))
                throw new Exception("缺少必要文件。");

            var js = System.IO.File.ReadAllText(KeyPath, Encoding.UTF8);
            var df = js.ToObject<DefaultConfiguration>();
            if (string.IsNullOrWhiteSpace(df.DefaultWSSUrl)
                || string.IsNullOrWhiteSpace(df.DefaultPublicKeyString))
                throw new Exception("文件内容有错误。");

            if (!df.DefaultWSSUrl.StartsWith("ws", StringComparison.InvariantCultureIgnoreCase))
                throw new NotSupportedException("不支持的协议。");

            df.DefaultPublicKey = df.DefaultPublicKeyString.ToBinary();
            Console.WriteLine(df.DefaultPublicKey.ToHex());
            return df;
        }
    }
}
