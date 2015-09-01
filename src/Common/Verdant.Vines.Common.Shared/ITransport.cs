using System;
using System.Text;

namespace Verdant.Vines.Common
{
    public delegate void MessageReceivedEventHandler(object sender, Guid fromNodeId, string payload);

    interface ITransport : IDisposable
    {
        void Open();
        void Close();

        bool IsConnected { get; }

        void Send(Guid destinationNodeId, string payload);
        void Send(Guid destinationNodeId, byte[] payload, int offset, int len);

        event MessageReceivedEventHandler OnMessageReceived;
    }
}
