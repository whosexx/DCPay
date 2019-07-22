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
    public class SecureKeyInfo
    {
        /// <summary>
        /// 平台id
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// 授权码
        /// </summary>
        public string AuthCode { get; set; }

        public string CheckSum { get; set; }

        public string ServerPublicKey { get; set; }

        public string ClientPrivateKey { get; set; }

        public string GetCheckSum()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(this.Platform);
            sb.AppendLine(this.AuthCode);
            sb.AppendLine(this.ServerPublicKey);
            sb.AppendLine(this.ClientPrivateKey);

            return sb.ToString().ToBytes().Sha256().ToHex();
        }

        public bool Verify()
        {
            if (string.Compare(this.GetCheckSum(), this.CheckSum, true) == 0)
                return true;

            return false;
        }
    }
}
