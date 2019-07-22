using CommandLine;
using StackExchange.Redis;
using System;
using System.Net;

namespace RedisHelper
{
    public enum Method
    {
        Export,
        Import
    }

    public class Options
    {
        [Option('h', "host", Required = false, HelpText = "Set output to verbose messages.")]
        public string Host { get; set; } = "127.0.0.1:6379";

        [Option('p', "password", Required = false, HelpText = "Set output to verbose messages.")]
        public string Password { get; set; }

        [Option('i', "index", Required = false, HelpText = "Set output to verbose messages.")]
        public int Index { get; set; } = 0;

        [Option('m', "method", Required = false, HelpText = "Set output to verbose messages.")]
        public Method Method { get; set; } = Method.Export;
    }

    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            //int port = 6379;
            //string host = "127.0.0.1";

            //int idx = 0;

            //Method cmd = Method.Export;
            //var cmds = CommandLine.Parser.Default.ParseArguments(args);
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       Console.WriteLine(o.Host  + ", " + o.Index + ", " + o.Method);
                       ConfigurationOptions op = new ConfigurationOptions();
                       op.EndPoints.Add(o.Host);
                       //op.ServiceName = o.Host;
                       op.Password = o.Password;
                       op.DefaultDatabase = o.Index;
                       var c = ConnectionMultiplexer.Connect(op);
                       //var db = c.GetDatabase();
                       Uri u = new Uri(o.Host);
                       var s = c.GetServer(u.Host, u.Port);
                       var keys = s.Keys(o.Index);
                       foreach (var key in keys)
                           Console.WriteLine(key);
                   });
        }
    }
}
