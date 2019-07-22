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
using DotNetty.Transport.Channels;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Tls;
using DotNetty.Codecs;
using DarkV.Extension.Collections;

namespace DCPay.Core.Channels
{
    public class TcpChannel : InitializedBase<Dictionary<string, string>>, IAsyncChannel
    {
        public virtual string Name { get; protected set; }
        public IChannel Channel { get; private set; }

        private MultithreadEventLoopGroup group;
        //protected TcpClient Inner { get; set; }

        protected SslStream Stream { get; set; }

        public IPAddress IP { get; protected set; }

        public int Port { get; protected set; }

        public bool Connected { get; protected set; }

        public bool UseSSL => this.Params.GetValue(nameof(this.UseSSL), false);

        public TcpChannel(string ip, int port)
        {
            this.IP = IPAddress.Parse(ip);
            this.Port = port;
            if (this.Port <= 0 || this.Port >= ushort.MaxValue)
                throw new Exception("端口不正确。");

            //this.Inner = new TcpClient(new IPEndPoint(IPAddress.Any, 11223))
            //{
            //    NoDelay = true,
            //    ReceiveTimeout = 10 * 1000,
            //    SendTimeout = 10 * 1000
            //};
            //this.Inner.Connect(this.IP, this.Port);

            //this.Stream = new SslStream(this.Inner.GetStream());
            //this.Stream.AuthenticateAsClient("*.n8.app");
            //this.ConnectedEvent?.Invoke(this);
        }

        protected override void InitializeInternal(Dictionary<string, string> arg)
        {
            base.InitializeInternal(arg);

            group = new MultithreadEventLoopGroup();

            try
            {
                var bootstrap = new Bootstrap();
                bootstrap.Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if(this.UseSSL)
                            pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings("")));

                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                        //pipeline.AddLast("encoder", new ModbusEncoder());
                        //pipeline.AddLast("decoder", new ModbusDecoder(false));

                        pipeline.AddLast("response", new ModbusResponseHandler());
                    }));
                MessageToByteEncoder
                //connectionState = ConnectionState.Pending;
                
                Channel = bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(Ip), Port)).Result;
                this.Connected = true;
                //connectionState = ConnectionState.Connected;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public virtual event Action<IAsyncChannel, Packet> ReceivePacketEvent;
        public virtual event Action<IAsyncChannel> ConnectedEvent;
        public virtual event Action<IAsyncChannel> DisConnectedEvent;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (this.Stream != null)
                {
                    this.Stream.Dispose();
                    this.Stream = null;
                }

                //if (this.Inner != null)
                //{
                //    this.Inner.Dispose();
                //    this.Inner = null;
                //}
            }
        }

        public void Send(Packet pkg)
        {

        }
    }
}
