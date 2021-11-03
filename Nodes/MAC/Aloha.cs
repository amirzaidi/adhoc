using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AdHocMAC.Nodes.MAC
{
    class Aloha : IMACProtocol<Packet>
    {
        public Action<Packet> SendAction;

        public void Send(Packet OutgoingPacket)
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
