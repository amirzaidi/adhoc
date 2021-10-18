using System;

namespace AdHocMAC.Nodes.MAC
{
    class CarrierSensingNonPersistent : CarrierSensing
    {
        public CarrierSensingNonPersistent(Action<Packet> Transmit) : base(Transmit)
        {
        }
    }
}
