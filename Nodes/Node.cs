using AdHocMAC.Nodes.MAC;
using AdHocMAC.Simulation;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes
{
    /// <summary>
    /// This node thinks there is a real network layer attached to it.
    /// </summary>
    class Node : INode<Packet>
    {
        private readonly int mId;
        private readonly ISendProtocol<Packet> mSendProtocol;

        public Node(int Id, ISendProtocol<Packet> SendProtocol)
        {
            mId = Id;
            mSendProtocol = SendProtocol;
        }


        /// <summary>
        /// This can do background work, such as coming up with random packets to send.
        /// </summary>
        public async Task Loop(CancellationToken Token)
        {
            while (!Token.IsCancellationRequested)
            {
                await Task.Delay(1000);

                // Send a Hello World packet to the node with ID+1.
                Send(new Packet
                {
                    From = mId,
                    To = mId + 1,
                    Data = "Hello World!"
                });
            }
        }

        private void Send(Packet OutgoingPacket)
        {
            // The basic node code does not bother with how sending is handled.
            mSendProtocol.Send(OutgoingPacket);
        }

        /*
         * From here on, the events should use CSMA/CA to handle what's happening.
         */
        public void OnReceiveStart()
        {
            mSendProtocol.OnChannelBusy();
        }

        public void OnReceiveCollide()
        {
            // This one is mutually exclusive with OnReceiveSuccess.
            // Either this event happens, or the other one, but not both.
            // For now, we can probably ignore this one.
        }

        public void OnReceiveSuccess(Packet IncomingPacket)
        {
            if (IncomingPacket.To == mId && mSendProtocol.OnReceive(IncomingPacket))
            {
                // To-Do: What do we want to do with "good" packets?
            }
        }

        public void OnReceiveEnd()
        {
            mSendProtocol.OnChannelFree();
        }
    }
}
