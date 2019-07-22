using CommandLine;
using DarkV.Extension.Json;
using JZPay.Core;
using JZPay.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DCPayClient.Windows
{
    public class BaseOptions
    {
        [Option('p', "platform", Required = false, HelpText = "platform id.")]
        public string Platform { get; set; }

        [Option('a', "authcode", Required = false, HelpText = "platform authcode.")]
        public string AuthCode { get; set; }

        [Option('c', "autocreate", Required = false, HelpText = "auto create order.")]
        public bool AutoCreate { get; set; } = false;
    }

    [Verb("corder", HelpText = "Add file contents to the index.")]
    class CreateOrderOptions
    {
        //normal options here
    }

    [Verb("chaxun", HelpText = "Record changes to the repository.")]
    class QueryOrderOptions
    {
        //commit options here
        [Option('i', "orderid", Required = false, HelpText = "order id.")]
        public string OrderId { get; set; }
    }

    [Verb("chaxunj", HelpText = "Clone a repository into a new directory.")]
    class QueryOrderByJOrderIdOptions
    {
        //clone options here
        [Option('j', "jorderid", Required = false, HelpText = "jorder id.")]
        public string JOrderId { get; set; }
    }

    class Program
    {
        const string json = "request.json";
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            try
            {
                DCPayHelper.ReceiveMessageEvent += DCPayHelper_ReceiveMessageEvent;
                DCPayHelper.ReceiveUnknownDataEvent += DCPayHelper_ReceiveUnknownDataEvent;
                string platfrom = "1";
                string authcode = "123456";
                Parser.Default.ParseArguments<BaseOptions>(args)
                   .WithParsed<BaseOptions>(o =>
                   {
                       if (!string.IsNullOrWhiteSpace(o.Platform))
                           platfrom = o.Platform;

                       if (!string.IsNullOrWhiteSpace(o.AuthCode))
                           authcode = o.AuthCode;

                       if (o.AutoCreate)
                       {
                           Task.Factory.StartNew(() =>
                            {
                                var request = File.ReadAllText(json, Encoding.UTF8).ToObject<OrderRequestInfo>();
                                while (true)
                                {
                                    do
                                    {
                                        try
                                        {
                                            request.JOrderId = Guid.NewGuid().ToString("N");
                                            Logger.Debug($"创建订单[{request.JOrderId}]");
                                            var r = DCPayHelper.CreateOrder(request, platfrom, authcode);
                                            if (r.Code != 0)
                                            {
                                                Logger.Debug($"创建订单[{request.JOrderId}]失败，失败消息：[{r.Message}]");
                                                break;
                                            }
                                            Logger.Debug(r.ToJSON());
                                        }
                                        catch (Exception ex) { Logger.Error(ex.ToString()); }

                                    } while (false);

                                    System.Threading.Thread.Sleep(60 * 1000);
                                }
                            }, TaskCreationOptions.LongRunning);
                       }
                   });

                string orderid = "";
                string jorderid = "";
                while (true)
                {
                    Console.Write("等待输入命令>");
                    string cmd = Console.ReadLine();
                    Parser.Default.ParseArguments<CreateOrderOptions, 
                        QueryOrderOptions, 
                        QueryOrderByJOrderIdOptions>(cmd.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    .MapResult(
                      (CreateOrderOptions opts) => 
                      {
                          var request = File.ReadAllText(json, Encoding.UTF8).ToObject<OrderRequestInfo>();
                          request.JOrderId = Guid.NewGuid().ToString("N");
                          jorderid = request.JOrderId;
                          Console.WriteLine($"创建订单[{jorderid}]");
                          var r = DCPayHelper.CreateOrder(request, platfrom, authcode);
                          if(r.Code != 0)
                          {
                              Console.WriteLine($"创建订单[{jorderid}]失败，失败消息：[{r.Message}]");
                              return false;
                          }
                          orderid = r.Data?.OrderId;
                          Console.WriteLine(r.ToJSON());
                          return true;
                      },

                      (QueryOrderOptions opts) => 
                      {
                          if (!string.IsNullOrWhiteSpace(opts.OrderId))
                              orderid = opts.OrderId;

                          Console.WriteLine($"查询订单[{orderid}]");
                          var or = DCPayHelper.QueryOrder(orderid, platfrom, authcode);
                          Console.WriteLine(or.ToJSON());
                          return true;
                      },

                      (QueryOrderByJOrderIdOptions opts) => 
                      {
                          if (!string.IsNullOrWhiteSpace(opts.JOrderId))
                              jorderid = opts.JOrderId;

                          Console.WriteLine($"查询J订单[{jorderid}]");
                          var or = DCPayHelper.QueryOrderByJOrderId(jorderid, platfrom, authcode);
                          Console.WriteLine(or.ToJSON());
                          return true;
                      },
                      errs => true);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }

            Console.ReadKey();
        }

        private static void DCPayHelper_ReceiveUnknownDataEvent(ResultInfo obj)
        {
            Console.WriteLine(obj.ToJSON());
        }

        private static ResultInfo DCPayHelper_ReceiveMessageEvent(ReceiveServerMessageEventArgs arg)
        {
            Console.WriteLine(arg.ToJSON());
            return ResultInfo.OK;
        }
    }
}
