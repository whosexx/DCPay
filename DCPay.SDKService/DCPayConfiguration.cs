using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace JZPay.SDKService
{
    public class DCPayConfiguration
    {
        protected static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();  

        public const string CString = "sdk.json";

        [JsonProperty(Required = Required.Always)]
        public string ListenUrl { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Callback { get; set; }

        private static DCPayConfiguration _config = null;

        public static DCPayConfiguration Default
        {
            get
            {
                if (_config == null)
                    _config = GetConfig();

                return _config;
            }
        }
        

        public static DCPayConfiguration GetConfig()
        {
            if (_config != null)
                return _config;

            if (!System.IO.File.Exists(CString))
                throw new System.IO.FileNotFoundException("没有发现配置文件。");

            var json = System.IO.File.ReadAllText(CString, Encoding.UTF8);
            _config = JsonConvert.DeserializeObject<DCPayConfiguration>(json);
            
            return _config;
        }
    }
}