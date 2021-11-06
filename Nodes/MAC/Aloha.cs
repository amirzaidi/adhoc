using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class Aloha : IMACProtocol<Packet>
    {
        public Func<Packet, CancellationToken, Task> SendAction = async (p, ct) => { };

        private Task mSend = Task.CompletedTask;
        private int mBacklog;

        public void SendInBackground(Packet OutgoingPacket, Action OnTimeout, CancellationToken Token)
        {
            Interlocked.Increment(ref mBacklog);
            mSend = mSend.ContinueWith(async _ =>
            {
                await SendAction(OutgoingPacket, Token);
                Interlocked.Decrement(ref mBacklog);
            });
        }

        public int BacklogCount()
        {
            return mBacklog;
        }

        public PacketType OnReceive(Packet IncomingPacket)
        {
            return PacketType.New;
        }

        public void OnChannelBusy()
        {
        }

        public void OnChannelFree()
        {
        }
    }
}
