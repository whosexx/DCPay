using DarkV.Extension.Bases;
using DCPay.Core.RPC;
using DCPay.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace DCPay.Core
{
    public interface IObject
    {

    }

    public enum MethodType
    {
        CreateOrder,
        QueryOrder,
        QueryOrderByJOrderId,
        NotifyUrl
    }

    public class ReceiveServerMessageEventArgs : EventArgs
    {
        public MethodType MethodType { get; set; }

        public IObject Data { get; set; }

        public T GetData<T>() => (T)this.Data;
    }

    public class ResultInfo : ResultInfo<object>
    {
        public static readonly new ResultInfo OK = new ResultInfo
        {
            Code = 0,
            Message = "ok",
            Data = new object { }
        };

        public static implicit operator ResultInfo(RPCError rpc)
            => new ResultInfo { Code = rpc.Code, Message = rpc.Message, Data = rpc.Data };
    }

    public class ResultInfo<T>
    {
        public static readonly ResultInfo<T> OK = new ResultInfo<T>
        {
            Code = 0,
            Message = "ok",
        };

        public int Code { get; set; }

        public string Message { get; set; }

        public T Data { get; set; }

        public static implicit operator ResultInfo<T>(RPCError rpc)
            => new ResultInfo<T> { Code = rpc.Code, Message = rpc.Message };
    }

    public interface IPay : IDisposable
    {
        event Action<IPay, ResultInfo> ChannelErrorEvent;

        event Func<IPay, NotifyInfo, ResultInfo> ReceiveServerMessageEvent;

        //bool ExchangePublicKey(string authcode);

        //bool Login();

        ResultInfo<OrderResponseInfo> CreateOrder(OrderRequestInfo request);

        ResultInfo<OrderInfo> QueryOrder(string order_id);

        ResultInfo<OrderInfo> QueryOrderByJOrderId(string jorder_id);
    }
}
