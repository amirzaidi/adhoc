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
        protected bool mIsChannelBusy;

        // We always have CA enabled to make it easier to implement.
        //private readonly CollisionAvoidance mCA = new CollisionAvoidance();

        public abstract Task Send(Packet OutgoingPacket, CancellationToken Token);

        public bool OnReceive(Packet IncomingPacket)
        {
            if (IncomingPacket.Data == "ACK")
            {
                // To-Do: Use the CA protocol.
                return false;
            }

            // We do not have ACKs yet, so we can assume every incoming packet is valid.
            return true;
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
    }
}
