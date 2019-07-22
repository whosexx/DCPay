using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JZPay.Core.Services;
using JZPay.Core.Configurations;

namespace JZPay.Core
{
    public class SecurePayHelper<T> where T : class, ISecureProtocol, new()
    {
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly object dLocker = new object();
        private readonly Dictionary<string, object> hLockers = new Dictionary<string, object>();
        
        protected Func<ISecureProtocol, ReceiveServerMessageEventArgs, ResultInfo> ReceiveMessageHandler;
        public event Action<T, ResultInfo> ReceiveUnknownDataEvent;

        public SecurePayHelper(Func<ISecureProtocol, ReceiveServerMessageEventArgs, ResultInfo> handler)
        {
            this.ReceiveMessageHandler = handler;
            SecureClientManage<T>.Default.ReceiveUnknownDataEvent += (arg1, arg2) => this.ReceiveUnknownDataEvent?.Invoke(arg1, arg2);;
        }

        public virtual T GetPayClient(string platform, string authcode)
        {
            object locker = null;
            lock(dLocker)
            {
                if(!hLockers.TryGetValue(platform, out locker))
                {
                    locker = new object();
                    hLockers[platform] = locker;
                }
            }

            var pay = SecureClientManage<T>.Default.GetConnection(platform, authcode);
            if (pay != null)
                return pay;

            lock (locker)
            {
                pay = SecureClientManage<T>.Default.GetConnection(platform, authcode);
                if (pay != null)
                    return pay;

                return SecureClientManage<T>.Default.CreateConnection(platform, authcode, this.ReceiveMessageHandler);
            }
        }

        //private ResultInfo Client_ReceiveServerMessageEvent(ISecureProtocol arg1, ReceiveServerMessageEventArgs arg2)
        //{
        //    if (this.ReceiveMessageHandler == null)
        //        return new ResultInfo { Code = -1003, Message = "客户端没有处理通知。" };

        //    return this.ReceiveMessageHandler?.Invoke(arg1, arg2);
        //}
    }
}
