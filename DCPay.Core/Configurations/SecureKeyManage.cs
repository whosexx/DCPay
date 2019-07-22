using DarkV.Extension.Bases;
using DarkV.Extension.Crypto;
using DarkV.Extension.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace JZPay.Core.Configurations
{
    public class SecureKeyManage : List<SecureKeyInfo>
    {
        public const string KeysPath = "keys.json";

        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        public static string SettingPath { get; set; } = "";
        public static string FullPath 
            => Path.Combine(string.IsNullOrWhiteSpace(SettingPath) ? AppDomain.CurrentDomain.BaseDirectory : SettingPath, KeysPath);

        private static readonly object dLocker = new object();
        private static SecureKeyManage _default = null;
        public static SecureKeyManage Default
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

        public SecureKeyInfo this[string platform]
        {
            get => this.FirstOrDefault(m => m.Platform == platform);
            set
            {
                if (value == null
                    || !value.Verify())
                    throw new ArgumentException("参数校验失败，无法设置。");

                lock (dLocker)
                {
                    if (this.Any(m => m.Platform == platform))
                        throw new Exception($"已经存在平台[{platform}]的密钥，无法添加。");

                    this.Add(value);
                }

                this.Save();
            }
        }

        public SecureKeyManage() { }

        private static readonly SemaphoreSlim sLocker = new SemaphoreSlim(1, 1);
        public void Save()
        {
            if (sLocker.Wait(10))
            {
                try
                {
                    using (FileStream fs = new FileStream(FullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        fs.SetLength(0);
                        var js = this.ToJSON().ToBytes();
                        fs.Write(js, 0, js.Length);
                    }
                }
                finally { sLocker.Release(); }
            }
        }

        public void Reload()
        {
            _default = null;
        }

        public static SecureKeyManage Load()
        {
            if (!File.Exists(FullPath))
            {
                var s = new SecureKeyManage();
                s.Save();
                return s;
            }

            var js = File.ReadAllText(FullPath, Encoding.UTF8);
            var secure = js.ToObject<SecureKeyManage>();
            bool save = false;
            foreach(var s in secure.ToList())
            {
                if (!s.Verify())
                {
                    
                    secure.Remove(s);
                    save = true;
                }
            }

            if (save)
                secure.Save();

            return secure;
        }

        public static new void Clear()
        {
            if (File.Exists(FullPath))
                File.Delete(FullPath);

            _default = null;
        }

        public void Clear(string platform)
        {
            this.RemoveAll(m => m.Platform == platform);
            this.Save();
        }
    }
}
