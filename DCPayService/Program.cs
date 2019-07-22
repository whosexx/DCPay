//using Pay.Core.IO;
//using System;
//using System.IO;
//using System.Net.Security;
//using System.Net.Sockets;
//using System.Security.Authentication;
//using System.Security.Cryptography.X509Certificates;
//using System.Threading.Tasks;

//namespace TLSService
//{
//    class Program
//    {
//        //static X509Certificate x509Certificate;
//        static async Task Main(string[] args)
//        {
//            TcpListener listener = new TcpListener(System.Net.IPAddress.Any, 12345);

//            try
//            {
//                listener.Start(1024);
//                Console.WriteLine("服务已经启动。");
//                //x509Certificate = X509Certificate.CreateFromCertFile("cert.pfx");
//                while (true)
//                {
//                    var t = await listener.AcceptTcpClientAsync();
//                    Handle(t);
//                }
//            }
//            catch (Exception ex) { Console.WriteLine(ex.ToString()); }

//            listener.Stop();
//            Console.ReadKey();
//        }

//        static void Handle(TcpClient t)
//        {
//            try
//            {
//                Console.WriteLine($"接收到新下连接[{t.Client.RemoteEndPoint}]");
//                var s = t.GetStream();
//                while (true)
//                {
//                    if (t.Client.Poll(1000, SelectMode.SelectRead))
//                    {
//                        int len = t.Client.Available;
//                        Console.WriteLine("数据长度：" + len + ", " + t.Available);
//                        if (len == 0)
//                        {
//                            Console.WriteLine("已经断开连接");
//                            break;
//                            //socket连接已断开
//                        }

//                        var buff = new byte[len];
//                        var r = s.Read(buff, 0, len);
//                        if (r != len)
//                            throw new Exception("读取的长度不一致。");

//                        //if (!Pay.Core.Stream.Packet.TryParse(buff, out var msg))
//                        //    throw new Exception("收到的数据有问题。");

//                        //Console.WriteLine(System.Text.Encoding.UTF8.GetString(msg.Data));

//                        //string str = "我收到啦。。哈哈。。SB";
//                        //var ret = ((Pay.Core.Stream.Packet)str).ToBytes();
//                        //s.Write(ret, 0, ret.Length);
//                    }
//                }
//                t.Close();

//            }catch(Exception ex) { Console.WriteLine(ex.ToString()); }
//        }
//    }
//}
using DarkV.Extension.Bases;
using DarkV.Extension.Json;
using Fleck;
using Org.BouncyCastle.Crypto.Parameters;
using JZPay.Core;
using JZPay.Core.Channels;
using JZPay.Core.RPC;
using JZPay.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using DarkV.Extension.Crypto;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DCPayService
{
    class Program
    {
        public const string ClientPublicKey = "3a803b8ca325442911b2a9be87235c61f0ffa2aa73fe3658b14cf8743ba9b5a6";
        public const string ClientPrivateKey = "70f04cb67ca4b1243090384023805f8ef5d13cf3a0dfd1b213548d3730df69c0";

        //public const string ServerPublicString = "1e4dc662f94ef45c97590f15f126279feb8b6a7bd7408bf4f631200289c74ce4";
        public const string ServerPrivateString = "8cfbd4564389c998f5bfd1b5496a1600f4bc012e08d5dc7a9fdf1bd59f2ee853";

        public const string RevcivePublicKey = "baae146db61c47fe3677cfb013862c1993f3eeb9ef214d03338f40be565678f9";
        //static readonly Ed25519KeyPair ServerPair = new Ed25519KeyPair
        //{
        //    Private = new Ed25519PrivateKeyParameters(ServerPrivateString.ToBinary(), 0),
        //    //Public = new Ed25519PublicKeyParameters(ServerPublicString.ToBinary(), 0)
        //};

        static void Main(string[] args)
        {
            FleckLog.Level = LogLevel.Debug;

            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://0.0.0.0:10000");
            server.Start(socket =>
            {
                socket.OnOpen = () => //当建立Socket链接时执行此方法
                {
                    var data = socket.ConnectionInfo; //通过data可以获得这个链接传递过来的Cookie信息，用来区分各个链接和用户之间的关系（如果需要后台主动推送信息到某个客户的时候，可以使用Cookie）
                    Console.WriteLine("Open!");
                    allSockets.Add(socket);
                };

                socket.OnClose = () =>// 当关闭Socket链接十执行此方法
                {

                    Console.WriteLine("Close!");
                    allSockets.Remove(socket);
                };

                socket.OnMessage = message =>// 接收客户端发送过来的信息
                {
                    //Console.WriteLine(message);
                    Packet p = message;
                    RPCPacket rpc = p.Message;
                    Console.WriteLine(rpc.Method);
                    Console.WriteLine(rpc.Params.ToJSON());

                    //回复包
                    RPCPacket ret = new RPCPacket(rpc.Id);
                    do
                    {
                        var pk = new Ed25519PublicKeyParameters(RevcivePublicKey.ToBinary(), 0);
                        if (!pk.Verify(p.GetSignatureData(), p.Signature.ToBinary()))
                        {
                            ret.Error = new RPCError
                            {
                                Code = -2,
                                Message = "签名验证失败。",
                            };

                            break;
                        }

                        switch (rpc.Method.ToLower())
                        {
                            case "exchangepublickey":
                            {
                                var ci = rpc.Params.ToString().ToObject<CipherInfo>();
                                var aes = ServerPrivateString.ToBinary().GetShareKey(ci.PublicKey.ToBinary());
                                Console.WriteLine("AES Key: " + aes.ToHex());
                                var txt = Encoding.UTF8.GetString(ci.Cipher.ToBinary().AESDecrypt(aes));
                                var split = txt.Split('_');
                                Console.WriteLine(split[0] + "_" + split[1] + "_" + split[2]);
                                if (string.Compare(split[0], ci.PublicKey, true) != 0)
                                {
                                    ret.Error = new RPCError
                                    {
                                        Code = -1,
                                        Message = "公钥不匹配。",
                                    };
                                    break;
                                }

                                ret.Result = new CipherInfo
                                {
                                    Cipher = ClientPublicKey.ToBinary().AESEncrypt(aes).ToHex(),
                                };
                                break;
                            }
                            case "login":
                            {
                                var order = (rpc.Params as JToken)["platform_id"].ToString();
                                Console.WriteLine(order);
                                ret.Result = new object ();
                                break;
                            }
                            case "createorder":
                            {
                                var order = rpc.Params.ToString().ToObject<OrderRequestInfo>();
                                ret.Result = new OrderResponseInfo
                                {
                                    OrderType = order.OrderType,
                                    OrderId = Guid.NewGuid().ToString("N"),
                                    PaymentUrl = "www.test.com"
                                };
                                break;
                            }
                            case "queryorder":
                            {
                                var order = (rpc.Params as JToken)["orderId"].ToString();
                                Console.WriteLine(order);
                                ret.Result = new OrderInfo
                                {
                                    OrderId = order,
                                    JOrderId = Guid.NewGuid().ToString("N"),
                                    PayWay = PayWay.AliPay,
                                    OrderType = OrderType.充值,
                                    Amount = 100,
                                    Currency = "CNY",
                                    Fee = 1.5m,
                                    Status = OrderStatus.已创建,
                                    JExtra = "123"
                                };
                                break;
                            }
                            case "queryorderbyjorderid":
                            {
                                var order = (rpc.Params as JToken)["jOrderId"].ToString();
                                Console.WriteLine(order);
                                ret.Result = new OrderInfo
                                {
                                    OrderId = order,
                                    JOrderId = Guid.NewGuid().ToString("N"),
                                    PayWay = PayWay.AliPay,
                                    OrderType = OrderType.充值,
                                    Amount = 100,
                                    Currency = "CNY",
                                    Fee = 1.5m,
                                    Status = OrderStatus.已创建,
                                    JExtra = "123"
                                };
                                break;
                            }
                            default:
                                ret.Error = new RPCError
                                {
                                    Code = -1,
                                    Message = "Not Support Method.",
                                };
                                break;
                        }
                    } while (false);

                    Packet s_pkg = new Packet(ret.ToString(), p.PlatformId);
                    s_pkg.Signature = new Ed25519PrivateKeyParameters(ClientPrivateKey.ToBinary(), 0).Sign(s_pkg.GetSignatureData()).ToHex();
                    socket.Send(s_pkg.ToJSON());
                };
            });

            var input = Console.ReadLine();
            while (input != "exit")
            {
                foreach (var socket in allSockets.ToList())
                {
                    socket.Send(input);
                }
                input = Console.ReadLine();
            }
        }
    }
}
