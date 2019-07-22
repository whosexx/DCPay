using DarkV.Extension.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace JZPay.Demo
{
    public static class HashHelper
    {
        private const string Key = "108a14f12e6b8f2ef54d63b73b535680";
        public static readonly byte[] Keys = Key.ToBytes();//.ToBinary();

        public static byte[] HMACSHA256(this byte[] data)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Keys))
            {
                hmac.Initialize();
                return hmac.ComputeHash(data);
            }
        }
    }
}
