using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class Aloha : IMACProtocol<Packet>
    {
        private Func<Packet, CancellationToken, Task> mSendAction = async (p, ct) => { };

        private Task mSend = Task.CompletedTask;
        private int mBacklog;

        public void SetSendAction(Func<Packet, CancellationToken, Task> SendAction)
        {
            mSendAction = SendAction;
        }

        public void SendInBackground(Packet OutgoingPacket, Action OnTimeout, CancellationToken Token)
        {
            Interlocked.Increment(ref mBacklog);
            mSend = mSend.ContinueWith(async _ =>
            {
                await mSendAction(OutgoingPacket, Token);
                Interlocked.Decrement(ref mBacklog);
            });
        }

        public int BacklogCount()
        {
            return mBacklog;
        }

        public void RemoveFromBacklog(int To, int Seq)
        {
        }

        public PacketType OnReceive(Packet IncomingPacket)
        {
            return IncomingPacket.To == Packet.BROADCAST_TO_ID
                ? PacketType.Broadcast
                : PacketType.NewPacket;
        }

        public void OnChannelBusy()
        {
        }

        public void OnChannelFree()
        {
        }
    }
}
