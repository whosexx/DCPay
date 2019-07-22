using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using DarkV.Extension.Bases;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using DarkV.Extension.Collections;

namespace JZPay.Core.Channels.Pools
{
    public class PoolBase<T> : InitializedBase<Dictionary<string, string>>, IPool<T> where T : class, IDisposable
    {
        static PoolBase() => ThreadPool.SetMinThreads(100, 100);


        public virtual event Action<T> CreateInstanceEvent;

        protected virtual Func<T> CreateInstanceHandle { get; set; }

        public virtual int MaxCount { get; protected set; } = 10;

        public bool SafeExit => this.Params.GetValue(nameof(this.SafeExit), true);

        private int _check_interval = 0;
        public int CheckInterval
        {
            get
            {
                if (this._check_interval <= 0)
                {
                    this._check_interval = this.Params.GetValue(nameof(this.CheckInterval), 5);
                    if (this._check_interval <= 0)
                        throw new ArgumentException(nameof(this.CheckInterval));
                }
                return this._check_interval;
            }
        }

        protected readonly object pLocker = new object();
        protected Dictionary<T, PoolObject<T>> Instances { get; set; } = new Dictionary<T, PoolObject<T>>();
        
        public PoolBase(int max, Func<T> func)
        {
            this.MaxCount = max;
            this.CreateInstanceHandle = func ?? throw new ArgumentNullException("func参数不能为空");
        }

        protected override void InitializeInternal(Dictionary<string, string> arg)
        {
            base.InitializeInternal(arg);
            if(this.Instances.Count <= 0)
            {
                var t = this.GetAvailableInstance();
                if (t == null)
                    throw new Exception("无法创建实例。");

                this.Return(t);
            }

        }

        protected virtual T TryCreateInstance()
        {
            if (this.Instances.Count >= this.MaxCount)
                return null;

            lock (this.pLocker)
            {
                if (this.Instances.Count >= this.MaxCount)
                    return null;


                var f = this.CreateInstanceHandle.Invoke();
                if (f == null)
                    throw new Exception("创建实例失败。");

                this.CreateInstanceEvent?.Invoke(f);
                Logger.Info($"成功创建Channel Instance[{f.GetHashCode()}].");
                this.Instances[f] = new PoolObject<T>(f);
                return f;
            }
        }

        public static readonly int TryCount = 5;
        protected virtual T GetAvailableInstance()
        {
            lock (this.pLocker)
            {
                int cnt = TryCount;
                while (cnt-- > 0
                    && this.Instances.Count > 0)
                {
                    var f = this.Instances.Values.FirstOrDefault(m => m.Available && m.TryGet(1));
                    if (f != null)
                    {
                        Logger.Debug($"获取Pool Object[{f.Object.GetHashCode()}]");
                        return f.Object;
                    }

                    Thread.Sleep(1);
                }
            }

            return this.TryCreateInstance();
        }

        protected virtual void RemoveInstance(T t)
        {
            lock (this.pLocker)
            {
                this.Instances.Remove(t);
                t.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(disposing)
                this.Clear();
        }

        public virtual T Take() => this.GetAvailableInstance();

        protected DateTime LastCheck { get; set; } = DateTime.Now;
        protected virtual void CheckAvailableObject()
        {
            if (this.LastCheck.AddSeconds(this.CheckInterval) > DateTime.Now)
                return;

            if (this.Instances.Count <= 1)
                return;

            lock(this.pLocker)
            {
                foreach(var p in this.Instances.Values.ToList())
                {
                    //30s不活动，删除掉。
                    if(p.Last.AddSeconds(30) < DateTime.Now)
                        RemoveInstance(p.Object);
                }
            }
        }

        public virtual void Return(T t)
        {
            if (this.Instances.TryGetValue(t, out var value))
            {
                value.Release();
                Logger.Info($"释放Pool Ojbect[{t.GetHashCode()}]");
            }
            
            this.CheckAvailableObject();
        }

        public virtual void Clear()
        {
            if (this.Instances != null 
                && this.Instances.Count > 0)
            {
                bool safe = this.SafeExit;
                lock (this.pLocker)
                {
                    foreach (var p in this.Instances)
                    {
                        if (safe)
                            p.Value.TryGet();
                        p.Value.Dispose();
                    }
                    this.Instances.Clear();
                }
            }
        }
    }
}
