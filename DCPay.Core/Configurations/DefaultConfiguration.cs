using DarkV.Extension.Bases;
using DarkV.Extension.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace JZPay.Core.Configurations
{
    public class DefaultConfiguration
    {
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public const string DefaultPath = "default.json";

        public static string SettingPath { get; set; } = "";
        
        public static string FullPath
            => Path.Combine(string.IsNullOrWhiteSpace(SettingPath) ? AppDomain.CurrentDomain.BaseDirectory : SettingPath, DefaultPath);

        private static readonly object dLocker = new object();
        private static DefaultConfiguration _default = null;
        public static DefaultConfiguration Default
        {
            get
            {
                if (_default != null)
                    return _default;

                lock (dLocker)
                {
                    if (_default != null)
                        return _default;

                    _default = Load();
                }

                return _default;
            }
        }

        public string DefaultWSSUrl { get; set; }

        public string DefaultPublicKeyString { get; set; }

        [JsonIgnore]
        public byte[] DefaultPublicKey { get; set; }

        public static DefaultConfiguration Load()
        {
            if (!File.Exists(FullPath))
                throw new Exception($"缺少必要文件[{FullPath}]。");

            var js = File.ReadAllText(FullPath, Encoding.UTF8);
            var df = js.ToObject<DefaultConfiguration>();
            if (string.IsNullOrWhiteSpace(df.DefaultWSSUrl)
                || string.IsNullOrWhiteSpace(df.DefaultPublicKeyString))
                throw new Exception("文件内容有错误。");

            if (!df.DefaultWSSUrl.StartsWith("ws", StringComparison.InvariantCultureIgnoreCase))
                throw new NotSupportedException("不支持的协议。");

            df.DefaultPublicKey = df.DefaultPublicKeyString.ToBinary();
            return df;
        }
    }
}
