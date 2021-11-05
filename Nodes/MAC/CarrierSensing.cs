using System;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    /// <summary>
    /// Contains the algorithm to handle channel sensing.
    /// </summary>
    abstract class CarrierSensing : IMACProtocol<Packet>
    {
        // We always have CA enabled to make it easier to implement.
        private readonly CollisionAvoidance mCA = new CollisionAvoidance();
        private readonly Action<Packet> mTransmit;

        protected bool mIsChannelBusy;

        public CarrierSensing()
        {
            //mTransmit = Transmit;
        }

        abstract public Task Send(Packet OutgoingPacket);
        public bool OnReceive(Packet IncomingPacket)
        {
            return false;
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
