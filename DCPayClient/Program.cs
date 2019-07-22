using DarkV.Extension.Bases;
using DCPay.Core;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.WebSockets;
using System.Threading.Tasks;
using DarkV.Extension.Json;
using Rebex.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;
using System.Threading;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;
using System.Collections;
using DCPay.Core.Configurations;

namespace DCPayClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //ClientWebSocket webSocket = new ClientWebSocket();
                //webSocket.Options.KeepAliveInterval = new TimeSpan(0, 0, 0, 0, 5000);
                //webSocket.Options.SetBuffer(8 * 1024, 8 * 1024);
                //webSocket.ConnectAsync(new Uri("wss://test.n8.app:443"), CancellationToken.None).Wait() ;
                //Console.WriteLine("连接完成1。");
                //ClientWebSocket webSocket1 = new ClientWebSocket();
                //webSocket1.Options.KeepAliveInterval = new TimeSpan(0, 0, 0, 0, 5000);
                //webSocket1.Options.SetBuffer(8 * 1024, 8 * 1024);
                //webSocket1.ConnectAsync(new Uri("wss://test.n8.app:443"), CancellationToken.None).Wait();
                //Console.WriteLine("连接完成2。");
                //Console.Read();
                //return;
                ////var key = Utils.GetRandomKeyPair();
                ////Console.WriteLine(key.Public.GetEncoded().ToHex());
                ////Console.WriteLine(key.Private.GetEncoded().ToHex());
                //Ed25519PrivateKeyParameters curve = new Ed25519PrivateKeyParameters("aa8d035517cc253119e5764b5631b7c3c738456071d2ea041baec33657f14929".ToBinary(), 0);
                //curve.FromPrivateKey("aa8d035517cc253119e5764b5631b7c3c738456071d2ea041baec33657f14929".ToBinary());
                //Console.WriteLine(curve.GeneratePublicKey().GetEncoded().ToHex());
                //Console.WriteLine(curve.GetSharedSecret("332867303356c2e0dd88c60baa18258a9b34793fbf3fa1c92c289ee621812a0c".ToBinary()).ToHex());

                //curve.FromPrivateKey("aa8d035517cc253119e5764b5631b7c3c738456071d2ea041baec33657f14929".ToBinary());
                //Console.WriteLine(curve.GetPublicKey().ToHex());
                //Console.WriteLine(curve.GetSharedSecret("48b33dfbdd1cffaf189bb5589dc5ee04209d0011a0ce574717f514057eaffd7a".ToBinary()).ToHex());
                //Ed25519PrivateKeyParameters pri = new Ed25519PrivateKeyParameters("70f04cb67ca4b1243090384023805f8ef5d13cf3a0dfd1b213548d3730df69c0".ToBinary(), 0);
                //Console.WriteLine(pri.GeneratePublicKey().GetEncoded().ToHex());
                //return;


                //PayClient.Clear();

                ThreadPool.SetMinThreads(100, 100);
                PayClient pay = new PayClient("1", SecureKeyInfo.Default);
                pay.ReceiveServerMessageEvent += Pay_ReceiveServerMessageEvent;
                pay.ChannelErrorEvent += Pay_ChannelErrorEvent;
                pay.Initialize(new Dictionary<string, string>
                {
                    [nameof(PayClient.UseConnectionPools)] = false.ToString()
                });

                //var ex = pay.ExchangePublicKey("123456");
                //Console.WriteLine(ex);

                //if(ex)
                //{
                //    pay = new PayClient("1");
                //    pay.ReceiveServerMessageEvent += Pay_ReceiveServerMessageEvent;
                //    pay.ChannelErrorEvent += Pay_ChannelErrorEvent;
                //    pay.Initialize();
                //}

                string orderid = "";
                string jorderid = Guid.NewGuid().ToString("N");
                while (true)
                {
                    string cmd = Console.ReadLine();
                    if (cmd == "q")
                        break;

                    //Console.WriteLine(pay.ExchangePublicKey("1234"));
                    //Console.Read();
                    switch (cmd)
                    {
                        case "COrder":
                        {
                            var r = pay.CreateOrder(new DCPay.Core.Services.OrderRequestInfo
                            {
                                JUserId = "tyx",
                                JUserIp = "127.0.0.1",
                                JOrderId = jorderid,
                                Amount = 10,
                                Currency = "CNY",
                                JExtra = "d091de28a842d2cfec397668a6b",
                                OrderType = DCPay.Core.Services.OrderType.充值,
                                PayWay = DCPay.Core.Services.PayWay.AliPay
                            });
                            orderid = r.Data.OrderId;
                            Console.WriteLine(r.ToJSON());
                            break;
                        }
                        case "QOrder":
                        {
                            var or = pay.QueryOrder(orderid);
                            Console.WriteLine(or.ToJSON());
                            break;
                        }
                        case "QJOrder":
                        {
                            var or = pay.QueryOrderByJOrderId(jorderid);
                            Console.WriteLine(or.ToJSON());
                            break;
                        }
                        //case "Login":
                        //{
                        //    Console.WriteLine("Login: " + pay.Login());
                        //    break;
                        //}
                        case "Loop":
                        {
                            int cnt = 10;
                            while (cnt-- > 0)
                            {
                                Task.Run(() =>
                                {
                                    try
                                    {
                                        var or = pay.QueryOrder(orderid);
                                        Console.WriteLine(or.ToJSON());
                                    }
                                    catch (Exception eee) { Console.WriteLine(eee.ToString()); }
                                });
                            }
                            break;
                        }
                    }
                }

                pay.Dispose();
                Console.WriteLine("Hello World!");
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }

            Console.ReadKey();
        }

        private static void Pay_ChannelErrorEvent(IProtocol arg1, ResultInfo arg2)
        {
            Console.WriteLine("通道错误：" + arg2.ToJSON());
            arg1.Dispose();
        }

        private static ResultInfo Pay_ReceiveServerMessageEvent(IProtocol arg1, DCPay.Core.Services.NotifyInfo arg2)
        {
            Console.WriteLine(arg2.ToJSON());
            return ResultInfo.OK;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
