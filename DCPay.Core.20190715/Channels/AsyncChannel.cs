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
using DCPay.Core.RPC;
using DCPay.Core.Channels.Pools;

namespace DCPay.Core.Channels
{
    public class AsyncChannel : InitializedBase<Dictionary<string, string>>, IAsyncChannel
    {
        public const int DefaultBufferSize = 8 * 1024;
        public const int MinBufferSize = 1024;

        public virtual string Name { get; protected set; }

        public int KeepAliveInterval => this.Params.GetValue(nameof(this.KeepAliveInterval), 5 * 1000);

        public int SendTimeout => this.Params.GetValue(nameof(this.SendTimeout), 10 * 1000);

        private int _send_buff_size = 0;
        public int SendBufferSize
        {
            get {
                if (_send_buff_size <= 0)
                {
                    this._send_buff_size = this.Params.GetValue(nameof(this.SendBufferSize), DefaultBufferSize);
                    if (this._send_buff_size <= MinBufferSize)
                        throw new ArgumentException(nameof(SendBufferSize));
                }

                return this._send_buff_size;
            }
        }

        private int _recv_buff_size = 0;
        public int RecvBufferSize
        {
            get
            {
                if (_recv_buff_size <= 0)
                {
                    this._recv_buff_size = this.Params.GetValue(nameof(this.RecvBufferSize), DefaultBufferSize);
                    if (this._recv_buff_size <= MinBufferSize)
                        throw new ArgumentException(nameof(RecvBufferSize));
                }

                return this._recv_buff_size;
            }
        }

        public virtual bool Connected { get; protected set; }

        protected ClientWebSocket Client { get; set; }
       
        public string WSSUrl { get; protected set; }

        public AsyncChannel(string url)
        {
            this.Name = Guid.NewGuid().ToString();
            this.WSSUrl = url;
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("链接有问题。");
        }

        public AsyncChannel(string name, string url)
        {
            this.Name = name;
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("名称不能为空。");

            this.WSSUrl = url;
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("链接有问题。");
        }

        protected override void InitializeInternal(Dictionary<string, string> arg)
        {
            base.InitializeInternal(arg);

            this.Client = new ClientWebSocket();
            this.Client.Options.KeepAliveInterval = new TimeSpan(0, 0, 0, 0, this.KeepAliveInterval);
            this.Client.Options.SetBuffer(this.RecvBufferSize, this.SendBufferSize);

            Logger.Debug($"ConnectAsync: {this.WSSUrl}");
            this.Client.ConnectAsync(new Uri(this.WSSUrl), CancellationToken.None).Wait();
            this.Connected = true;
            Logger.Debug($"ConnectAsync服务[{this.WSSUrl}]完成。");

            Task.Factory.StartNew(async() => 
            {
                Logger.Debug("接收线程已启动。");
                byte[] buff = new byte[this.RecvBufferSize];
                MemoryStream ms = null;
                while (!this.IsDisposed)
                {
                    try
                    {
                        if(this.Client.State != WebSocketState.Open)
                        {
                            Logger.Warn($"通道[{this.Name}]状态[{this.Client.State}]异常.");
                            this.Client.Abort();
                            break;
                        }

                        var r = await this.Client.ReceiveAsync(new ArraySegment<byte>(buff), CancellationToken.None);
                        if(r.MessageType == WebSocketMessageType.Close)
                        {
                            if (ms != null)
                            {
                                ms.Dispose();
                                ms = null;
                            }

                            Logger.Warn($"通道[{this.Name}]收到服务端发来的关闭连接的请求，关闭状态[{r.CloseStatus}]和原因[{r.CloseStatusDescription}]，准备关闭连接[{this.IsDisposed}][{this.Client.State}]。");
                            continue;
                        }

                        if(r.EndOfMessage)
                        {
                            string txt = "";

                            do
                            {
                                if (ms == null)
                                {
                                    txt = Encoding.UTF8.GetString(buff, 0, r.Count);
                                    break;
                                }

                                ms.Write(buff, 0, r.Count);
                                txt = Encoding.UTF8.GetString(ms.ToArray());
                                ms.Dispose();
                                ms = null;
                            } while (false);
                            
                            if(string.IsNullOrWhiteSpace(txt))
                            {
                                Logger.Warn($"通道[{this.Name}]没有收到任何数据，重新收包。");
                                continue;
                            }

                            try
                            {
                                var pkg = txt.ToObject<Packet>();
                                if(pkg == null)
                                {
                                    Logger.Warn("反序列化有问题。");
                                    continue;
                                }
                                
                                Logger.Debug($"通道[{this.Name}]收到业务包：" + pkg.PlatformId + ": " + ((RPCPacket)pkg.Message).ToJSON());
                                this.ReceivePacketEvent?.Invoke(this, pkg);
                            }
                            catch(Exception exf) { Logger.Error($"反序列化失败，可能是垃圾数据[{txt}]: [{exf}]"); }

                            continue;
                        }

                        if (ms == null)
                            ms = new MemoryStream();

                        ms.Write(buff, 0, r.Count);
                    }
                    catch(OperationCanceledException)
                    {
                        Logger.Warn("任务取消");
                        break;
                    }
                    catch(WebSocketException web) when ((uint)web.ErrorCode == 0x80004005)
                    {
                        Logger.Warn("websocket出错了：" + web.ToString());
                        break;
                    }
                    catch (Exception ex) { Logger.Error("处理消息出错了：" + ex.ToString()); }
                }
                Logger.Warn($"通道[{this.Name}]的接收消息线程已经退出。");

                this.Connected = false;
                this.DisConnectedEvent?.Invoke(this);
            }, TaskCreationOptions.LongRunning);
            
            this.ConnectedEvent?.Invoke(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Client != null)
                {
                    if(this.Client.State == WebSocketState.Open)
                        this.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "释放资源", CancellationToken.None).Wait(1000);
                    this.Client.Abort();
                    this.Client.Dispose();
                    this.Client = null;
                }
            }

            base.Dispose(disposing);
        }

        public virtual event Action<IAsyncChannel, Packet> SendPacketEvent;
        public virtual event Action<IAsyncChannel, Packet> ReceivePacketEvent;

        public virtual event Action<IAsyncChannel> ConnectedEvent;
        public virtual event Action<IAsyncChannel> DisConnectedEvent;

        public void Send(Packet pkg)
        {
            this.CheckDispose();

            if (pkg == null)
                throw new ArgumentNullException(nameof(pkg));

            if (this.Client.State != WebSocketState.Open)
            {
                Logger.Warn($"通道[{this.Name}]状态[{this.Client.State}]异常，无法发送消息.");
                this.Client.Abort();
                this.Connected = false;
                throw new Exception($"通道[{this.Name}]处于关闭状态，无法发送消息。");
            }

            Logger.Debug($"通道[{this.Name}]发送业务包：" + pkg.PlatformId + ": " + ((RPCPacket)pkg.Message).ToJSON());
            this.SendPacketEvent?.Invoke(this, pkg);
            lock (this.Client)
            {
                var bs = pkg.ToBytes();
                int loop = 1;
                if (bs.Length > this.SendBufferSize)
                    loop = (bs.Length + this.SendBufferSize - 1) / this.SendBufferSize;
                
                for (int i = 0; i < loop; i++)
                {
                    int offset = this.SendBufferSize * i;
                    int count = bs.Length - offset;
                    count = count > this.SendBufferSize ? this.SendBufferSize : count;

                    this.Client.SendAsync(new ArraySegment<byte>(bs, offset, count),
                        WebSocketMessageType.Text, (i == (loop - 1)),
                        CancellationToken.None).Wait(this.SendTimeout);
                }
            }
        }
    }
}
