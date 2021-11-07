using AdHocMAC.Nodes.MAC;
using AdHocMAC.Simulation;
using AdHocMAC.Utility;
using System;
using System.Collections.Generic;
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
        private const bool DEBUG = true;

        private readonly List<string> mPacketLog = new List<string>();

        private readonly int mId;
        private readonly int mNodeCount;
        private readonly IMACProtocol<Packet> mMACProtocol;
        private readonly Random mRNG;

        private readonly double mMsgGenerationProb;

        private readonly BufferBlock<object> mEvents = new BufferBlock<object>();
        private int mSequenceNumber;

        public Node(int Id, int NodeCount, IMACProtocol<Packet> MACProtocol, Random RNG)
        {
            mId = Id;
            mNodeCount = NodeCount;
            mMACProtocol = MACProtocol;
            mRNG = RNG;

            mMsgGenerationProb = Configuration.CreateMessageChance(RNG.NextDouble());
        }

        /// <summary>
        /// This can do background work, such as coming up with random packets to send.
        /// </summary>
        public async Task Loop(CancellationToken Token)
        {
            // To-Do: Implement Routing Algorithm In Here.
            CancellationTokenSource wakeupCTS = null;
            while (!Token.IsCancellationRequested)
            {
                // Every time the event queue becomes empty, we wake up the node to do
                // its own logic sometime in the future.
                if (mEvents.Count == 0)
                {
                    // Add some randomness for timing.
                    wakeupCTS = new CancellationTokenSource();
                    var CT = CancellationTokenSource.CreateLinkedTokenSource(Token, wakeupCTS.Token).Token;

                    // We cancel this if something happened.
                    _ = Task.Delay(Configuration.NODE_WAKEUP_TIME_MS, CT).ContinueWith(_ =>
                    {
                        if (!CT.IsCancellationRequested)
                        {
                            mEvents.Post(new WakeupEvent());
                        }
                    });
                }

                try
                {
                    var ev = await mEvents.ReceiveAsync(Token);
                    wakeupCTS?.Cancel(); // Cancel the wakeup event.
                    await OnEvent(ev, Token);
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        private async Task OnEvent(object Event, CancellationToken Token)
        {
            // Handle all incoming messages and timeouts here.
            if (Event is PacketReceived evReceived)
            {
                var packet = evReceived.IncomingPacket;
                switch (mMACProtocol.OnReceive(packet))
                {
                    case PacketType.NewPacket:
                        // To-Do: What do we want to do with unseen packets, other than send an ACK?
                        EnqueueReplyACK(packet, Token);
                        if (DEBUG) Debug.WriteLine($"[R NEW] {mId}: [{packet.From}, {packet.Seq}: {packet.Data}]");

                        var log = $"R, {packet.From}, {packet.To}, {packet.Seq}";
                        log += $", {packet.RetryAttempts}, {packet.InitialUnixTimestamp}, {Timestamp.UnixMS()}";
                        mPacketLog.Add(log);
                        break;
                    case PacketType.DuplicatePacket:
                        // Send another ACK for these.
                        EnqueueReplyACK(packet, Token);
                        if (DEBUG) Debug.WriteLine($"[R OLD] {mId}: [{packet.From}, {packet.Seq}: {packet.Data}]");
                        break;
                    case PacketType.Broadcast:
                        if (DEBUG) Debug.WriteLine($"[B NEW] {mId}: [{packet.From}, {packet.Seq}: {packet.Data}]");
                        break;
                    case PacketType.ACK:
                        // Successful packet.
                        if (DEBUG) Debug.WriteLine($"[R ACK] {mId}: [{packet.From}, {packet.Seq}]");
                        break;
                }
            }
            else if (Event is FailedTransmission evTimeout)
            {
                var packet = evTimeout.OutgoingPacket;
                if (packet.RetryAttempts >= 15)
                {
                    mMACProtocol.RemoveFromBacklog(packet.To, packet.Seq);
                    if (DEBUG) Debug.WriteLine($"[S DROP] {mId}: [{packet.To}, {packet.Seq}: {packet.Data}]");
                }
                else
                {
                    packet.RetryAttempts += 1;
                    EnqueueSend(packet, Token);
                    if (DEBUG) Debug.WriteLine($"[S RETRY] {mId}: [{packet.To}, {packet.Seq}: {packet.Data}]");
                }
            }
            else if (Event is WakeupEvent)
            {
                // Nothing happened for a while, so lets execute the start of an algorithm.
                // Basic logic: whenever there is no message in the queue, we send a new one.
                if (mMACProtocol.BacklogCount() == 0 && mRNG.NextDouble() < mMsgGenerationProb)
                {
                    // Send a Hello World packet to the node with ID+1.
                    // The basic node code does not bother with how sending is handled.
                    if (DEBUG) Debug.WriteLine($"[S NEW] {mId}: Sequence {mSequenceNumber}");

                    if (mRNG.NextDouble() < 0.1)
                    {
                        // Send a broadcast 10% of the time instead of a regular packet.
                        EnqueueBroadcast($"Hello Everyone from {mId}!", Token);
                    }
                    else
                    {
                        EnqueueSend((mId + 1) % mNodeCount, $"Hello Node from {mId}!", Token);
                    }
                }
            }
        }

        // Call this only from within the Loop().
        private void EnqueueSend(int To, object Data, CancellationToken Token)
        {
            EnqueueSend(new Packet
            {
                From = mId,
                To = To,
                Seq = mSequenceNumber++,
                Data = Data,
                InitialUnixTimestamp = Timestamp.UnixMS(),
            }, Token);
        }

        // Call this only from within the Loop().
        private void EnqueueSend(Packet OutgoingPacket, CancellationToken Token)
        {
            var log = $"S, {OutgoingPacket.From}, {OutgoingPacket.To}, {OutgoingPacket.Seq}";
            log += $", {OutgoingPacket.RetryAttempts}, {OutgoingPacket.InitialUnixTimestamp}, {Timestamp.UnixMS()}";

            mPacketLog.Add(log);
            mMACProtocol.SendInBackground(
                OutgoingPacket,
                () => mEvents.Post(new FailedTransmission { OutgoingPacket = OutgoingPacket }),
                Token
            );
        }

        // Call this only from within the Loop().
        private void EnqueueBroadcast(object Data, CancellationToken Token)
        {
            var broadcast = new Packet
            {
                From = mId,
                To = Packet.BROADCAST_TO_ID,
                Seq = mSequenceNumber++,
                Data = Data,
                InitialUnixTimestamp = Timestamp.UnixMS(),
            };

            mMACProtocol.SendInBackground(
                broadcast,
                () => { },
                Token
            );
        }

        // Call this only from within the Loop().
        private void EnqueueReplyACK(Packet IncomingPacket, CancellationToken Token)
        {
            var ACK = new Packet
            {
                From = IncomingPacket.To,
                To = IncomingPacket.From,
                Seq = IncomingPacket.Seq,
                ACK = true,
                Data = "",
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
            // if (DEBUG) Debug.WriteLine($"{mId}: PACKET [{IncomingPacket.From} -> {IncomingPacket.To}: {IncomingPacket.Data}]");
            if (IncomingPacket.To == mId || IncomingPacket.To == Packet.BROADCAST_TO_ID)
            {
                mEvents.Post(new PacketReceived { IncomingPacket = IncomingPacket });
            }
        }

        public void OnReceiveEnd() => mMACProtocol.OnChannelFree();

        public int GetID() => mId;

        public List<string> GetLog() => mPacketLog;

        class PacketReceived
        {
            public Packet IncomingPacket;
        }

        class FailedTransmission
        {
            public Packet OutgoingPacket;
        }

        class WakeupEvent
        {
        }
    }
}
