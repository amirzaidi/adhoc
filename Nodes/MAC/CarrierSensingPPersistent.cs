using System;

namespace AdHocMAC.Nodes.MAC
{
    class CarrierSensingPPersistent : CarrierSensing
    {
        private readonly double mPPersistency;

        public CarrierSensingPPersistent(Action<Packet> Transmit, double PPersistency) : base(Transmit)
        {
            mPPersistency = PPersistency;
        }
    }
}
