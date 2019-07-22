using DarkV.Extension.Bases;
using DarkV.Extension.Crypto;
using DarkV.Extension.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DCPay.Core.Configurations
{
    public class SecureKeyManage : List<SecureKeyInfo>
    {
        public const string KeysPath = "keys.json";

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
                if (!value.Verify())
                    throw new ArgumentException("参数校验失败，无法设置。");

                lock (dLocker)
                {
                    this.RemoveAll(m => m.Platform == platform);
                    if (value == null)
                        return;
                    
                    this.Add(value);
                    this.Save();
                }
            }
        }
        public void Save()
        {
            lock (dLocker)
            {
                using (FileStream fs = new FileStream(KeysPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    fs.SetLength(0);
                    var js = this.ToJSON().ToBytes();
                    fs.Write(js, 0, js.Length);
                }
            }
        }

        public void Reload()
        {
            _default = null;
        }

        public static SecureKeyManage Load()
        {
            if (!File.Exists(KeysPath))
                return new SecureKeyManage();

            var js = File.ReadAllText(KeysPath, Encoding.UTF8);
            var secure = js.ToObject<SecureKeyManage>();

            return secure;
        }

        public static new void Clear()
        {
            if (File.Exists(KeysPath))
                File.Delete(KeysPath);

            _default = null;
        }

        public void Clear(string platform)
        {
            this.RemoveAll(m => m.Platform == platform);
            this.Save();
        }
    }
}
