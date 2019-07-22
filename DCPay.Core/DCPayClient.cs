using DarkV.Extension.Bases;
using DarkV.Extension.Json;
using Org.BouncyCastle.Crypto.Parameters;
using JZPay.Core.Channels;
using JZPay.Core.RPC;
using JZPay.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DarkV.Extension.Crypto;
using DarkV.Extension.Collections;
using System.Threading.Tasks;
using JZPay.Core.Configurations;
using JZPay.Core.Channels.Pools;

namespace JZPay.Core
{
    public class DCPayClient : SecureProtocolBase, IDCPay
    {
        public DCPayClient() { }

        public DCPayClient(string platform) 
            : base(platform)
        {

        }

        public DCPayClient(string platform, SecureKeyInfo secure)
            : base(platform, secure)
        {

        }
        
        #region API
        public ResultInfo<OrderResponseInfo> CreateOrder(OrderRequestInfo request)
            => this.Invoke<OrderResponseInfo>(new RPCPacket("createOrder", request), this.Channel);

        public ResultInfo<OrderInfo> QueryOrder(string orderId)
            => this.Invoke<OrderInfo>(new RPCPacket("queryOrder", new { orderId }), this.Channel);

        public ResultInfo<OrderInfo> QueryOrderByJOrderId(string jOrderId)
            => this.Invoke<OrderInfo>(new RPCPacket("queryOrderByJOrderId", new { jOrderId }), this.Channel);

        #endregion
    }
}
