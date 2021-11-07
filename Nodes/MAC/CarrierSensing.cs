using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    /// <summary>
    /// Contains the algorithm to handle channel sensing.
    /// </summary>
    abstract class CarrierSensing : IMACProtocol<Packet>
    {
        private const bool DEBUG = false;

        protected Func<Packet, CancellationToken, Task> mSendAction = async (p, ct) => Debug.WriteLine("[CSMA] Simulated Send");

        // We always have CA enabled to make it easier to implement.
        protected readonly Random mRNG;
        private readonly CollisionAvoidance mCA;

        private Task mSend = Task.CompletedTask;
        private bool mIsChannelBusy;

        public CarrierSensing(Random RNG)
        {
            mRNG = RNG;
            mCA = new CollisionAvoidance(new Random(RNG.Next()));
        }

        public void SetSendAction(Func<Packet, CancellationToken, Task> SendAction)
        {
            mSendAction = SendAction;
        }

        public void SendInBackground(Packet OutgoingPacket, Action OnTimeout, CancellationToken Token)
        {
            if (!OutgoingPacket.ACK)
            {
                mCA.CreatePacketTimer(OutgoingPacket.To, OutgoingPacket.Seq);
            }

            mSend = mSend.ContinueWith(async _ =>
            {
                if (!OutgoingPacket.ACK) // Regular packet.
                {
                    await SendWhenChannelFree(OutgoingPacket, Token);
                    _ = Task.Run(async () => await mCA.WaitPacketTimer(OutgoingPacket.To, OutgoingPacket.Seq, OnTimeout, Token));
                }
                else if (IsChannelBusy()) // ACK but channel is occupied.
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] ACK Cannot Be Sent");
                }
                else // ACK and channel is free.
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] ACK Sent");
                    await mSendAction(OutgoingPacket, Token);
                }
            });
        }

        public int BacklogCount()
        {
            return mCA.BacklogCount();
        }

        public void RemoveFromBacklog(int To, int Seq)
        {
            mCA.CancelPacketTimer(To, Seq);
        }

        protected abstract Task SendWhenChannelFree(Packet OutgoingPacket, CancellationToken Token);

        public PacketType OnReceive(Packet IncomingPacket)
        {
            if (IncomingPacket.ACK)
            {
                mCA.CancelPacketTimer(IncomingPacket.From, IncomingPacket.Seq);
                return PacketType.Control;
            }

            return mCA.TryAddUniquePacketId(IncomingPacket.From, IncomingPacket.Seq)
                ? PacketType.New
                : PacketType.Old;
        }

        // To-Do: Maybe combine OnChannelBusy and OnChannelFree into one method.
        public void OnChannelBusy()
        {
            mIsChannelBusy = true;
        }

        public void OnChannelFree()
        {
            mIsChannelBusy = false;
        }

        protected bool IsChannelBusy() => mIsChannelBusy;
    }
}
