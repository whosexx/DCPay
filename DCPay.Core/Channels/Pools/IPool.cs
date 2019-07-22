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
    public static class PoolExtensions
    {
        public static List<T> Take<T>(this IPool<T> pool, int cnt) where T : class
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));

            if (cnt <= 0 || cnt > pool.MaxCount)
                throw new ArgumentException(nameof(cnt));

            List<T> rets = new List<T>();
            bool get = true;
            try
            {
                for(int i = 0; i < cnt; i++)
                {
                    var r = pool.Take();
                    if (r == null)
                    {
                        get = false;
                        break;
                    }

                    rets.Add(r);
                }

                return rets;
            }
            finally
            {
                if(!get)
                    pool.Return(rets);
            }
        }

        public static void Return<T>(this IPool<T> pool, IEnumerable<T> objs) where T : class
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));

            if (objs == null)
                throw new ArgumentNullException(nameof(objs));

            foreach (var o in objs)
                pool.Return(o);
        }
    }

    public interface IPool<T> : IDisposable where T : class
    {
        event Action<T> CreateInstanceEvent;

        int MaxCount { get; }

        T Take();

        void Return(T obj);

        void Clear();
    }
}
