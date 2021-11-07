using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    interface IMACProtocol<T>
    {
        public void SetSendAction(Func<Packet, CancellationToken, Task> SendAction);

        /// <summary>
        /// Handles scheduling when to send the packet behind the scenes.
        /// </summary>
        public void SendInBackground(T OutgoingPacket, Action OnTimeout, CancellationToken Token);

        public int BacklogCount();

        public void RemoveFromBacklog(int To, int Seq);

        /// <summary>
        /// Checks the kind of packet, and returns true when the node should handle it.
        /// It may also initiate ACK routines in the background.
        /// </summary>
        public PacketType OnReceive(T IncomingPacket);

        /// <summary>
        /// The OnChannelBusy and OnChannelFree callbacks are used for scheduling.
        /// </summary>
        public void OnChannelBusy();

        public void OnChannelFree();
    }
}
