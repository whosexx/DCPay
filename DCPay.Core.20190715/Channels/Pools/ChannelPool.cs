using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using DarkV.Extension.Bases;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace DCPay.Core.Channels.Pools
{
    public class ChannelPool : PoolBase<IAsyncChannel>, IAsyncChannel
    {
        public virtual string Name { get; protected set; }

        public virtual bool Connected { get; protected set; } = true;
        

        public ChannelPool(int max, Func<IAsyncChannel> func)
            : base(max, func)
        {
            this.CreateInstanceEvent += ChannelPool_CreateInstanceEvent;
        }
        
        private void ChannelPool_CreateInstanceEvent(IAsyncChannel chan)
        {
            chan.ReceivePacketEvent += Channel_ReceivePacketEvent;
            chan.DisConnectedEvent += Channel_DisConnectedEvent;
            chan.ConnectedEvent += Channel_ConnectedEvent;
        }

        private void Channel_ConnectedEvent(IAsyncChannel obj)
        {
            this.ConnectedEvent?.Invoke(obj);
            //TODO:发送登陆命令
        }

        protected override void InitializeInternal(Dictionary<string, string> arg)
        {
            base.InitializeInternal(arg);
        }

        private void Channel_DisConnectedEvent(IAsyncChannel obj)
        {
            Logger.Warn($"连接[{obj.Name}]断开连接");
            this.RemoveInstance(obj);
            this.DisConnectedEvent?.Invoke(obj);
        }

        private void Channel_ReceivePacketEvent(IAsyncChannel arg1, Packet arg2)
        {
            this.ReceivePacketEvent?.Invoke(this, arg2);
        }

        
        public virtual event Action<IAsyncChannel, Packet> ReceivePacketEvent;
        
        public virtual event Action<IAsyncChannel> ConnectedEvent;
        public virtual event Action<IAsyncChannel> DisConnectedEvent;

        public void Send(Packet pkg)
        {
            this.CheckDispose();

            if (pkg == null)
                throw new ArgumentNullException(nameof(pkg));

            var c = this.Take();
            if (c == null)
                throw new Exception("没有可用的连接。");
                
            Logger.Debug($"Pool Count: {this.Instances.Count} - {Thread.CurrentThread.ManagedThreadId}");
            try { c.Send(pkg); }
            finally { this.Return(c); }
        }
    }
}
