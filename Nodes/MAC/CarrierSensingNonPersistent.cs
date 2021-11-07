using AdHocMAC.Utility;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class CarrierSensingNonPersistent : CarrierSensing
    {
        private const bool DEBUG = false;

        public CarrierSensingNonPersistent(Random RNG) : base(RNG)
        {
        }

        protected override async Task SendWhenChannelFree(Packet OutgoingPacket, CancellationToken Token)
        {
            var time = DateTime.Now;
            int attempt = 0;
            int upperBound = Configuration.MinSlotDelayUpperbound;

            while (!Token.IsCancellationRequested)
            {
                if (IsChannelBusy())
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] LineBusy at Attempt {attempt} to send");
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] Awaiting random");

                    var slots = mRNG.Next(0, upperBound);
                    await SyncedSlots.WaitUntilSlot(slots, Token);
                    attempt++;

                    // BEB on non-persistent CSMA.
                    if (upperBound < Configuration.MaxSlotDelayUpperbound)
                    {
                        upperBound *= 2;
                    }
                }
                else
                {
                    await mSendAction(OutgoingPacket, Token);
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] NonBusy Sent After: {(int)(DateTime.Now - time).TotalMilliseconds} ms");
                    break;
                }
            }
        }
    }
}
