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

        private const int RNG_UPPER_BOUND = 10000;

        private const double SLOT_SECONDS = 0.1;
        private const long SLOT_TICKS = (long)(TimeSpan.TicksPerSecond * SLOT_SECONDS);

        private readonly Random mRNG;
        private readonly double mPPersistency;

        public CarrierSensingPPersistent(Random RNG, double PPersistency) : base()
        {
            mRNG = RNG;
            mPPersistency = PPersistency;
        }

        protected override async Task SendWhenChannelFree(Packet OutgoingPacket, CancellationToken Token)
        {
            var time = DateTime.Now;
            int attempt = 0;

            if (!OutgoingPacket.ACK)
            {
                await WaitUntilNextSlot(Token);
            }

            while (!Token.IsCancellationRequested)
            {
                if (IsChannelBusy())
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] LineBusy at Attempt {++attempt} to send");
                }
                else if (!OutgoingPacket.ACK && mRNG.Next(0, RNG_UPPER_BOUND) / (double)RNG_UPPER_BOUND >= mPPersistency)
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] 1-P NonBusy NonSent Retrying");
                }
                else
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] P (ACK: {OutgoingPacket.ACK}) NonBusy Sent After: {(int)(DateTime.Now - time).TotalMilliseconds} ms");
                    await SendAction(OutgoingPacket, Token);
                    break;
                }

                await WaitUntilNextSlot(Token);
            }
        }

        private async Task WaitUntilNextSlot(CancellationToken Token)
        {
            var currTime = DateTime.Now;
            var ticksToNextSlot = SLOT_TICKS - (currTime.Ticks % SLOT_TICKS);
            var msToNextSlot = ticksToNextSlot / (double)TimeSpan.TicksPerMillisecond;

            await Task.Delay((int)Math.Ceiling(msToNextSlot), Token).IgnoreExceptions();
        }
    }
}
