using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DarkV.Extension.Json;
using DarkV.Extension.Net;
using JZPay.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JZPay.SDKService
{
    public class Program
    {
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            DCPayHelper.ReceiveMessageEvent += DCPayHelper_ReceiveMessageEvent;
            DCPayHelper.ReceiveUnknownDataEvent += DCPayHelper_ReceiveUnknownDataEvent;
            CreateWebHostBuilder(args).Run();
        }

        private static void DCPayHelper_ReceiveUnknownDataEvent(ResultInfo obj)
        {
            Logger.Error($"收到无法处理的数据格式[{System.Text.Encoding.UTF8.GetString(obj.Data as byte[])}]");
        }

        private static ResultInfo DCPayHelper_ReceiveMessageEvent(ReceiveServerMessageEventArgs arg)
        {
            try
            {
                using (NetClientV2 net = new NetClientV2())
                {
                    Logger.Debug($"向地址[{DCPayConfiguration.Default.Callback}]POST数据[{arg.Data.ToJSON()}]");
                    var js = net.Post(DCPayConfiguration.Default.Callback, arg.Data.ToJSON());
                    Logger.Debug($"服务返回数据[{js}]");
                    return js.ToObject<ResultInfo>();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("回调出错了：" + ex);
                return new ResultInfo { Code = -1000, Message = ex.Message };
            }
        }

        public static IWebHost CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .UseUrls(DCPayConfiguration.Default.ListenUrl)
                   .UseKestrel()
                   .UseStartup<Startup>()
                   .Build();
    }
}
