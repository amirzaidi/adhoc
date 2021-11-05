using System;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class CarrierSensingPPersistent : CarrierSensing
    {
        private readonly double mPPersistency;

        public CarrierSensingPPersistent( double PPersistency) : base()
        {
            mPPersistency = PPersistency;

        }
        override public async Task Send(Packet OutgoingPacket)
        {

            if (!mIsChannelBusy)
            {



            }
        }
    }
}
