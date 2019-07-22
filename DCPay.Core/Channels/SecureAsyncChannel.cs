using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using DarkV.Extension.Bases;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DarkV.Extension.Json;
using DarkV.Extension.Collections;
using Org.BouncyCastle.Crypto.Parameters;
using JZPay.Core;

namespace JZPay.Core.Channels
{
    public sealed class SecureAsyncChannel<T> : AsyncChannel<T> where T : class, IBitConverter, ISignature, new()
    {
        public override event Action<IAsyncChannel<T>, T> ReceivePacketEvent;

        public override event Action<IAsyncChannel<T>, T> SendPacketEvent;

        private Ed25519PublicKeyParameters PublicKey { get; set; }

        private Ed25519PrivateKeyParameters PrivateKey { get; set; }

        /// <summary>
        /// 需要服务器的公钥，自己的私钥
        /// </summary>
        /// <param name="url"></param>
        public SecureAsyncChannel(string url, byte[] client_prikey, byte[] server_pubkey)
            : base(url)
        {
            if (client_prikey == null
                || server_pubkey == null)
                throw new ArgumentException(nameof(client_prikey));

            //if(server_pubkey != null
            //    && server_pubkey.Length > 0)
            this.PublicKey = new Ed25519PublicKeyParameters(server_pubkey, 0);

            //if(client_prikey != null
            //    && client_prikey.Length > 0)
            this.PrivateKey = new Ed25519PrivateKeyParameters(client_prikey, 0);

            base.ReceivePacketEvent += SecureAsyncChannel_ReceivePacketEvent;
            base.SendPacketEvent += SecureAsyncChannel_SendPacketEvent;
        }

        //protected override void InitializeInternal(Dictionary<string, string> arg)
        //{
        //    base.InitializeInternal(arg);
        //}

        private void SecureAsyncChannel_SendPacketEvent(IAsyncChannel<T> channel, T pkg)
        {
            if (this.PublicKey != null)
            {
                //签名
                pkg.Signature = this.PrivateKey.Sign(pkg.GetSignatureData()).ToHex();
                Logger.Debug($"签名包[{pkg})][{pkg.Signature}]");
            }

            this.SendPacketEvent?.Invoke(channel, pkg);
        }

        private void SecureAsyncChannel_ReceivePacketEvent(IAsyncChannel<T> channel, T pkg)
        {
            if (this.PublicKey != null)
            {
                if(string.IsNullOrWhiteSpace(pkg.Signature))
                {
                    Logger.Error($"空白签名，丢弃包[{pkg}]");
                    return;
                }

                //验证签名
                if (this.PublicKey.Verify(pkg.GetSignatureData(), pkg.Signature.ToBinary()))
                {
                    Logger.Debug($"验证包的签名[{pkg})]通过");
                    this.ReceivePacketEvent?.Invoke(this, pkg);
                    return;
                }

                Logger.Warn($"验证包的签名[{pkg})]失败，丢弃。");
                return;
            }

            this.ReceivePacketEvent?.Invoke(this, pkg);
        }

    }
}
