using AdHocMAC.Nodes.MAC;
using AdHocMAC.Simulation;
using AdHocMAC.Utility;
using System;
using System.Diagnostics;
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
        private readonly IMACProtocol<Packet> mMACProtocol;
        private readonly Random mRNG;

        public Node(int Id, IMACProtocol<Packet> MACProtocol, Random RNG)
        {
            mId = Id;
            mMACProtocol = MACProtocol;
            mRNG = RNG;
        }


        /// <summary>
        /// This can do background work, such as coming up with random packets to send.
        /// </summary>
        public async Task Loop(CancellationToken Token)
        {
            while (!Token.IsCancellationRequested)
            {
                // To-Do: Put Routing Algorithm Here.

                await Task.Delay(mRNG.Next(0, 3000), Token).IgnoreExceptions();

                // Send a Hello World packet to the node with ID+1.
                Send(new Packet
                {
                    From = mId,
                    To = mId + 1,
                    Data = "Hello World!"
                });

                await Task.Delay(mRNG.Next(0, 7000), Token).IgnoreExceptions();
            }
        }

        private void Send(Packet OutgoingPacket)
        {
            // The basic node code does not bother with how sending is handled.
            mMACProtocol.Send(OutgoingPacket);
        }

        /*
         * From here on, the events should use CSMA/CA to handle what's happening.
         */
        public void OnReceiveStart()
        {
            mMACProtocol.OnChannelBusy();
        }

        public void OnReceiveCollide()
        {
            Debug.WriteLine($"{mId}: COLLISION");
            // This one is mutually exclusive with OnReceiveSuccess.
            // Either this event happens, or the other one, but not both.
            // For now, we can probably ignore this one.
        }

        public void OnReceiveSuccess(Packet IncomingPacket)
        {
            Debug.WriteLine($"{mId}: PACKET [{IncomingPacket.From} -> {IncomingPacket.To}: {IncomingPacket.Data}]");
            if (IncomingPacket.To == mId && mMACProtocol.OnReceive(IncomingPacket))
            {
                // To-Do: What do we want to do with "good" packets?
            }
        }

        public void OnReceiveEnd()
        {
            mMACProtocol.OnChannelFree();
        }

        public int GetID() => mId;
    }
}
