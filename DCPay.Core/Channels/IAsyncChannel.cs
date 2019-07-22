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
using JZPay.Core.Channels.Pools;

namespace JZPay.Core.Channels
{
    public interface IAsyncChannel<T> : IInitialize<Dictionary<string, string>>, IDisposable where T : IBitConverter
    {
        /// <summary>
        /// 通道名称
        /// </summary>
        string Name { get; }

        bool Connected { get; }

        event Action<IAsyncChannel<T>, T> ReceivePacketEvent;
        event Action<IAsyncChannel<T>, byte[]> ReceiveUnknownDataEvent;

        event Action<IAsyncChannel<T>> ConnectedEvent;

        event Action<IAsyncChannel<T>> DisConnectedEvent;

        void Send(T pkg);
    }
}
