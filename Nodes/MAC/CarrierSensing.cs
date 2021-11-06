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
        public Func<Packet, CancellationToken, Task> SendAction = async (p, ct) => Debug.WriteLine("[CSMA] Simulated Send");

        // We always have CA enabled to make it easier to implement.
        private readonly CollisionAvoidance mCA = new CollisionAvoidance();

        private Task mSend = Task.CompletedTask;
        private int mBacklog;
        private bool mIsChannelBusy;

        public void SendInBackground(Packet OutgoingPacket, Action OnTimeout, CancellationToken Token)
        {
            Interlocked.Increment(ref mBacklog);
            mSend = mSend.ContinueWith(async _ =>
            {
                await SendWhenChannelFree(OutgoingPacket, Token);
                mCA.StartPacketTimer(OutgoingPacket.To, OutgoingPacket.Seq, OnTimeout, Token);
                Interlocked.Decrement(ref mBacklog);
            });
        }

        public int BacklogCount()
        {
            return mBacklog + mCA.BacklogCount();
        }

        protected abstract Task SendWhenChannelFree(Packet OutgoingPacket, CancellationToken Token);

        public PacketType OnReceive(Packet IncomingPacket)
        {
            if (IncomingPacket.ACK)
            {
                mCA.StopPacketTimer(IncomingPacket.From, IncomingPacket.Seq);
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
