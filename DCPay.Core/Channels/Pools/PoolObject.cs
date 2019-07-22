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
    public class PoolObject<T> : DisposableBase where T : class, IDisposable
    {
        private SemaphoreSlim Semaphore { get; set; }
        
        public virtual bool Available { get; protected set; }

        public T Object { get; protected set; }

        public virtual DateTime Last { get; protected set; }

        public virtual bool TryGet() => this.TryGet(Timeout.Infinite);

        public virtual bool TryGet(int ms)
        {
            if(this.Semaphore.Wait(ms))
            {
                this.Available = false;
                this.Last = DateTime.Now;
                return true;
            }
            
            return false;
        }

        public virtual void Release()
        {
            if (this.Semaphore.CurrentCount <= 0)
            {
                this.Semaphore.Release();
                this.Available = true;
            }
        }

        public PoolObject(T obj)
        {
            this.Semaphore = new SemaphoreSlim(0, 1);
            this.Object = obj;
            this.Available = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (this.Object != null)
                {
                    this.Object.Dispose();
                    this.Object = null;
                }

                if (this.Semaphore != null)
                {
                    this.Semaphore.Dispose();
                    this.Semaphore = null;
                }
            }
        }
    }
}
