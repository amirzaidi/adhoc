using System;
using System.Collections.Generic;
using System.Text;

namespace AdHocMAC.Simulation
{
    interface INetworkEventLogger<T>
    {
        public void BeginReceive(T Receiver, T Sender);

        public void EndReceive(T Receiver, T Sender);

        public void BeginSend(T Sender);

        public void EndSend(T Sender);
    }
}
