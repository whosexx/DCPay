using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using DarkV.Extension.Bases;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace JZPay.Core.Channels.Pools
{
    public class ChannelPool<T> : PoolBase<IAsyncChannel<T>>, IAsyncChannel<T> where T : class, IBitConverter, new()
    {
        public virtual string Name { get; protected set; }

        public virtual bool Connected { get; protected set; } = true;
        

        public ChannelPool(int max, Func<IAsyncChannel<T>> func)
            : base(max, func)
        {
            this.CreateInstanceEvent += ChannelPool_CreateInstanceEvent;
        }
        
        private void ChannelPool_CreateInstanceEvent(IAsyncChannel<T> chan)
        {
            chan.ReceivePacketEvent += Channel_ReceivePacketEvent;
            chan.ReceiveUnknownDataEvent += Chan_ReceiveUnknownDataEvent;

            chan.DisConnectedEvent += Channel_DisConnectedEvent;
            chan.ConnectedEvent += Channel_ConnectedEvent;
        }

        private void Chan_ReceiveUnknownDataEvent(IAsyncChannel<T> arg1, byte[] arg2)
        {
            this.ReceiveUnknownDataEvent?.Invoke(arg1, arg2);
        }

        private void Channel_ConnectedEvent(IAsyncChannel<T> obj)
            => this.ConnectedEvent?.Invoke(obj);

        private void Channel_DisConnectedEvent(IAsyncChannel<T> obj)
        {
            Logger.Warn($"连接[{obj.Name}]断开连接");
            this.RemoveInstance(obj);
            this.DisConnectedEvent?.Invoke(obj);
        }

        private void Channel_ReceivePacketEvent(IAsyncChannel<T> arg1, T arg2)
            => this.ReceivePacketEvent?.Invoke(this, arg2);

        
        public virtual event Action<IAsyncChannel<T>, T> ReceivePacketEvent;
        public virtual event Action<IAsyncChannel<T>, byte[]> ReceiveUnknownDataEvent;

        public virtual event Action<IAsyncChannel<T>> ConnectedEvent;
        public virtual event Action<IAsyncChannel<T>> DisConnectedEvent;

        public void Send(T pkg)
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
