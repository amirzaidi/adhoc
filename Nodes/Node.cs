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

        private readonly BufferBlock<object> mEvents = new BufferBlock<object>();
        private int mSequenceNumber;

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
            long loopIteration = 0;
            while (!Token.IsCancellationRequested)
            {
                // Handle all incoming messages and timeouts here.
                while (mEvents.Count > 0)
                {
                    var ev = await mEvents.ReceiveAsync(Token);
                    if (ev is PacketReceived evReceived)
                    {
                        var packet = evReceived.IncomingPacket;
                        switch (mMACProtocol.OnReceive(packet))
                        {
                            case PacketType.New:
                                // To-Do: What do we want to do with unseen packets, other than send an ACK?
                                ReplyACK(packet, Token);
                                Debug.WriteLine($"[R NEW] {mId}: [{packet.From}, {packet.Seq}: {packet.Data}]");
                                break;
                            case PacketType.Old:
                                // Send another ACK for these.
                                ReplyACK(packet, Token);
                                Debug.WriteLine($"[R OLD] {mId}: [{packet.From}, {packet.Seq}: {packet.Data}]");
                                break;
                            case PacketType.Control:
                                // Successful packet.
                                Debug.WriteLine($"[R ACK] {mId}: [{packet.From}, {packet.Seq}]");
                                break;
                        }
                    }
                    else if (ev is FailedTransmission evTimeout)
                    {
                        var packet = evTimeout.OutgoingPacket;
                        EnqueueSend(packet, Token);
                        Debug.WriteLine($"[S RETRY] {mId}: [{packet.To}, {packet.Seq}: {packet.Data}]");
                    }
                }

                // To-Do: Implement Routing Algorithm Here.

                // Basic logic: whenever there is no message in the queue, we send a new one.
                if (mMACProtocol.BacklogCount() == 0)
                {
                    // Send a Hello World packet to the node with ID+1.
                    // The basic node code does not bother with how sending is handled.
                    Debug.WriteLine($"[S NEW] {mId}: Sequence {mSequenceNumber}");
                    EnqueueSend((mId + 1) % mNodeCount, $"Hello World from {mId}!", Token);
                }

                loopIteration += 1;
                await Task.Delay(mRNG.Next(50, 100), Token).IgnoreExceptions(); // Add some randomness for timing.
            }
        }

        // Call this only from within the Loop().
        private void EnqueueSend(int To, string Data, CancellationToken Token)
        {
            EnqueueSend(new Packet
            {
                From = mId,
                To = To,
                Seq = mSequenceNumber++,
                Data = Data
            }, Token);
        }

        // Call this only from within the Loop().
        private void EnqueueSend(Packet OutgoingPacket, CancellationToken Token)
        {
            mMACProtocol.SendInBackground(
                OutgoingPacket,
                () => mEvents.Post(new FailedTransmission { OutgoingPacket = OutgoingPacket }),
                Token
            );
        }

        // Call this only from within the Loop().
        private void ReplyACK(Packet IncomingPacket, CancellationToken Token)
        {
            var ACK = new Packet
            {
                From = IncomingPacket.To,
                To = IncomingPacket.From,
                Seq = IncomingPacket.Seq,
                ACK = true,
                Data = ""
            };

            mMACProtocol.SendInBackground(
                ACK,
                () => { },
                Token
            );
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

        // Keep this as simple as possible, by only giving the packets to Loop().
        public void OnReceiveSuccess(Packet IncomingPacket)
        {
            if (DEBUG) Debug.WriteLine($"{mId}: PACKET [{IncomingPacket.From} -> {IncomingPacket.To}: {IncomingPacket.Data}]");
            if (IncomingPacket.To == mId)
            {
                mEvents.Post(new PacketReceived { IncomingPacket = IncomingPacket });
            }
        }

        public void OnReceiveEnd() => mMACProtocol.OnChannelFree();

        public int GetID() => mId;

        class PacketReceived
        {
            public Packet IncomingPacket;
        }

        class FailedTransmission
        {
            public Packet OutgoingPacket;
        }
    }
}
