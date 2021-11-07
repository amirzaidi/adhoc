using AdHocMAC.Utility;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class CarrierSensingPPersistent : CarrierSensing
    {
        private const bool DEBUG = false;

        public CarrierSensingPPersistent(Random RNG) : base(RNG)
        {
        }

        protected override async Task SendWhenChannelFree(Packet OutgoingPacket, CancellationToken Token)
        {
            var time = DateTime.Now;
            int attempt = 0;

            while (!Token.IsCancellationRequested)
            {
                await SyncedSlots.WaitUntilSlot(0, Token);

                if (IsChannelBusy())
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] LineBusy at Attempt {++attempt} to send");
                }
                else if (mRNG.NextDouble() >= Configuration.PPersistency)
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] 1-P NonBusy NonSent Retrying");
                }
                else
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] P (ACK: {OutgoingPacket.ACK}) NonBusy Sent After: {(int)(DateTime.Now - time).TotalMilliseconds} ms");
                    await mSendAction(OutgoingPacket, Token);
                    break;
                }
            }
        }
    }
}
