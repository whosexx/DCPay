//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.IO;
//using System.Net.Sockets;
//using DarkV.Extension.Bases;
//using System.Net;
//using System.Net.Security;
//using System.Security.Cryptography.X509Certificates;
//using Pay.Core.IO;
//using System.Net.WebSockets;

//namespace Pay.Core.Channels
//{
//    public class TcpChannel : InitializedBase<Dictionary<string, string>>, IAsyncChannel
//    {
//        public virtual string Name { get; protected set; }

//        protected TcpClient Inner { get; set; }

//        protected SslStream Stream { get; set; }

//        public IPAddress IP { get; protected set; }

//        public int Port { get; protected set; }

//        public TcpChannel(string ip, int port)
//        {
//            this.IP = IPAddress.Parse(ip);
//            this.Port = port;
//            if (this.Port <= 0 || this.Port >= ushort.MaxValue)
//                throw new Exception("端口不正确。");

//            this.Inner = new TcpClient(new IPEndPoint(IPAddress.Any, 11223))
//            {
//                NoDelay = true,
//                ReceiveTimeout = 10 * 1000,
//                SendTimeout = 10 * 1000
//            };
//            this.Inner.Connect(this.IP, this.Port);

//            this.Stream = new SslStream(this.Inner.GetStream());
//            this.Stream.AuthenticateAsClient("*.n8.app");
//            this.ConnectedEvent?.Invoke(this);
//        }

//        public virtual event Action<IAsyncChannel, Packet> ReceivePacketEvent;
//        public virtual event Action<IAsyncChannel> ConnectedEvent;
//        public virtual event Action<IAsyncChannel> DisConnectedEvent;

//        protected override void Dispose(bool disposing)
//        {
//            base.Dispose(disposing);
//            if (disposing)
//            {
//                if (this.Stream != null)
//                {
//                    this.Stream.Dispose();
//                    this.Stream = null;
//                }

//                if (this.Inner != null)
//                {
//                    this.Inner.Dispose();
//                    this.Inner = null;
//                }
//            }
//        }

//        public void Send(Packet pkg)
//        {
            
//        }
//    }
//}
