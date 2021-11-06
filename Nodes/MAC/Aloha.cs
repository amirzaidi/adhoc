using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class Aloha : IMACProtocol<Packet>
    {
        public Func<Packet, CancellationToken, Task> SendAction = async (p, ct) => { };

        private Task mSend = Task.CompletedTask;

        public void SendInBackground(Packet OutgoingPacket, CancellationToken Token)
        {
            mSend = mSend.ContinueWith(async _ => await SendAction(OutgoingPacket, Token));
        }

        public bool OnReceive(Packet IncomingPacket)
        {
            return true;
        }

        public void OnChannelBusy()
        {
        }

        public void OnChannelFree()
        {
        }
    }
}
