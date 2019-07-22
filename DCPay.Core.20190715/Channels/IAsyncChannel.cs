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
using DCPay.Core.Channels.Pools;

namespace DCPay.Core.Channels
{
    public interface IAsyncChannel : IInitialize<Dictionary<string, string>>, IDisposable
    {
        /// <summary>
        /// 通道名称
        /// </summary>
        string Name { get; }

        bool Connected { get; }

        event Action<IAsyncChannel, Packet> ReceivePacketEvent;

        event Action<IAsyncChannel> ConnectedEvent;

        event Action<IAsyncChannel> DisConnectedEvent;

        void Send(Packet pkg);
    }
}
