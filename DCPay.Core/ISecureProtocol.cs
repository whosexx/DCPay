using DarkV.Extension.Bases;
using DarkV.Extension.Json;
using JZPay.Core.Configurations;
using JZPay.Core.RPC;
using JZPay.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace JZPay.Core
{
    public interface IObject
    {

    }

    public class ReceiveServerMessageEventArgs : EventArgs
    {
        public string Method { get; set; }

        public string Params { get; set; }

        public IObject Data { get; set; }

        public T GetData<T>() => this.Params.ToObject<T>();
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

    public interface ISecureProtocol : IInitialize<Dictionary<string, string>>, IDisposable
    {
        event Action<ISecureProtocol, ResultInfo> ChannelErrorEvent;

        event Func<ISecureProtocol, ReceiveServerMessageEventArgs, ResultInfo> ReceiveServerMessageEvent;
        
        string Platform { get; set; }

        SecureKeyInfo SecureKey { get; set; }

        DateTime Last { get; }

        ResultInfo<SecureKeyInfo> ExchangePublicKey(string authcode);
    }

    public interface IDCPay
    {
        ResultInfo<OrderResponseInfo> CreateOrder(OrderRequestInfo request);

        ResultInfo<OrderInfo> QueryOrder(string order_id);

        ResultInfo<OrderInfo> QueryOrderByJOrderId(string jorder_id);
    }
}
