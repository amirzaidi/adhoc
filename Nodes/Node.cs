using AdHocMAC.Nodes.MAC;
using AdHocMAC.Simulation;
using AdHocMAC.Utility;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace AdHocMAC.Nodes
{
    /// <summary>
    /// This node thinks there is a real network layer attached to it.
    /// </summary>
    class Node : INode<Packet>
    {
        private const bool DEBUG = false;

        private readonly int mId;
        private readonly int mNodeCount;
        private readonly IMACProtocol<Packet> mMACProtocol;
        private readonly Random mRNG;

        private readonly BufferBlock<Packet> mPackets = new BufferBlock<Packet>();

        public Node(int Id, int NodeCount, IMACProtocol<Packet> MACProtocol, Random RNG)
        {
            mId = Id;
            mNodeCount = NodeCount;
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

                while (mPackets.Count > 0)
                {
                    var packet = await mPackets.ReceiveAsync(Token);

                    // To-Do: What do we want to do with "good" packets?
                    Debug.WriteLine($"{mId}: PACKET FOR ME [{packet.From}: {packet.Data}]");
                }

                await Task.Delay(mRNG.Next(0, 3000), Token).IgnoreExceptions();

                // Send a Hello World packet to the node with ID+1.
                // The basic node code does not bother with how sending is handled.
                mMACProtocol.SendInBackground(new Packet
                {
                    From = mId,
                    To = (mId + 1) % mNodeCount,
                    Data = $"Hello World from {mId}!"
                }, Token);

                await Task.Delay(mRNG.Next(0, 7000), Token).IgnoreExceptions();
            }
        }

        /*
         * From here on, the events should use CSMA/CA to handle what's happening.
         */
        public void OnReceiveStart() => mMACProtocol.OnChannelBusy();

        public void OnReceiveCollide()
        {
            if (DEBUG) Debug.WriteLine($"{mId}: COLLISION");
            // This one is mutually exclusive with OnReceiveSuccess.
            // Either this event happens, or the other one, but not both.
            // For now, we can probably ignore this one.
        }

        public void OnReceiveSuccess(Packet IncomingPacket)
        {
            if (DEBUG) Debug.WriteLine($"{mId}: PACKET [{IncomingPacket.From} -> {IncomingPacket.To}: {IncomingPacket.Data}]");
            if (IncomingPacket.To == mId && mMACProtocol.OnReceive(IncomingPacket))
            {
                mPackets.Post(IncomingPacket);
            }
        }

        public void OnReceiveEnd() => mMACProtocol.OnChannelFree();

        public int GetID() => mId;
    }
}
