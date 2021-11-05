using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class CarrierSensingPPersistent : CarrierSensing
    {
        private readonly double mPPersistency;

        public CarrierSensingPPersistent(double PPersistency) : base()
        {
            mPPersistency = PPersistency;
        }

        public override async Task Send(Packet OutgoingPacket, CancellationToken Token)
        {
            if (!mIsChannelBusy)
            {

            }
        }
    }
}
