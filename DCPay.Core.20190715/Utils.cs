using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Rebex.Security.Cryptography;
using Org.BouncyCastle.Math.EC.Rfc8032;

namespace DCPay.Core
{
    //public class Ed25519KeyPair
    //{
    //    public Ed25519PublicKeyParameters Public { get; set; }

    //    public Ed25519PrivateKeyParameters Private { get; set; }

    //    public Ed25519KeyPair() { }

    //    public Ed25519KeyPair(byte[] pubkey, byte[] prikey)
    //    {
    //        this.Public = new Ed25519PublicKeyParameters(pubkey, 0);
    //        this.Private = new Ed25519PrivateKeyParameters(prikey, 0);
    //    }


    //    public static implicit operator Ed25519KeyPair(AsymmetricCipherKeyPair pair)
    //        => new Ed25519KeyPair
    //        {
    //            Public = (Ed25519PublicKeyParameters)pair.Public,
    //            Private = (Ed25519PrivateKeyParameters)pair.Private,
    //        };

    //    public static implicit operator AsymmetricCipherKeyPair(Ed25519KeyPair pair)
    //        => new AsymmetricCipherKeyPair(pair.Public, pair.Private);
    //}

    public static class Utils
    {
        public static byte[] GetRandomPrivateKey()
        {
            byte[] pk = new byte[32];
            Ed25519.GeneratePrivateKey(new SecureRandom(), pk);
            return pk;
            //Ed25519KeyPairGenerator keyGenerator = new Ed25519KeyPairGenerator();
            //keyGenerator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
            //return keyGenerator.GenerateKeyPair();
        }

        public static byte[] GetEd25519PublicKey(this byte[] prik) => new Ed25519PrivateKeyParameters(prik, 0).GeneratePublicKey().GetEncoded();

        public static byte[] GetCurve25519PublicKey(this byte[] prik)
        {
            var curve = new Curve25519();
            curve.FromPrivateKey(prik);
            return curve.GetPublicKey();
        }

        public static byte[] Sign(this Ed25519PrivateKeyParameters cipher, byte[] data)
        {
            Ed25519Signer s = new Ed25519Signer();
            s.Init(true, cipher);
            s.BlockUpdate(data, 0, data.Length);
            return s.GenerateSignature();
        }

        public static bool Verify(this Ed25519PublicKeyParameters cipher, byte[] data, byte[] signature)
        {
            Ed25519Signer s = new Ed25519Signer();
            s.Init(false, cipher);
            s.BlockUpdate(data, 0, data.Length);
            return s.VerifySignature(signature);
        }


        public static byte[] GetShareKey(this byte[] prikey, byte[] curve_pubkey)
        {
            Curve25519 curve = new Curve25519();
            curve.FromPrivateKey(prikey);

            return curve.GetSharedSecret(curve_pubkey);
        }
    }
}
