using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class Aloha : IMACProtocol<Packet>
    {
        public Func<Packet, CancellationToken, Task> SendAction = async (p, ct) => { };

        public async Task Send(Packet OutgoingPacket, CancellationToken Token)
        {
            await SendAction(OutgoingPacket, Token);
        }

        public bool OnReceive(Packet IncomingPacket)
        {
            return false;
        }

        public void OnChannelBusy()
        {
        }

        public void OnChannelFree()
        {
        }
    }
}
