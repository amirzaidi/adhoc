using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class Aloha : IMACProtocol<Packet>
    {
        public Action<Packet> SendAction;

        public async Task Send(Packet OutgoingPacket)
        {
            SendAction(OutgoingPacket);
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
