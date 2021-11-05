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

        private readonly Random mRNG;
        private readonly int mMinDelay, mMaxDelay;

        private readonly SemaphoreSlim mLock = new SemaphoreSlim(1);
        private Task mSend = Task.CompletedTask;

        public CarrierSensingNonPersistent(Random RNG, int MinDelay, int MaxDelay) : base()
        {
            mRNG = RNG;
            mMinDelay = MinDelay;
            mMaxDelay = MaxDelay;
        }

        public override async Task Send(Packet OutgoingPacket, CancellationToken Token)
        {
            await mLock.WaitAsync();
            mSend = mSend.ContinueWith(async _ => await SendWhenChannelFree(OutgoingPacket, Token));
            mLock.Release();
        }

        private async Task SendWhenChannelFree(Packet OutgoingPacket, CancellationToken Token)
        {
            var time = DateTime.Now;
            int attempt = 0;

            while (!Token.IsCancellationRequested)
            {
                if (mIsChannelBusy)
                {
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] LineBusy at Attempt {attempt} to send");
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] Awaiting random");
                    var sleeper = Task.Delay(mRNG.Next(mMinDelay, mMaxDelay), Token).IgnoreExceptions();
                    await sleeper;
                    attempt++;
                }
                else
                {
                    await SendAction(OutgoingPacket, Token);
                    if (DEBUG) Debug.WriteLine($"[{OutgoingPacket.From}] NonBusy Sent After: {(int)(DateTime.Now - time).TotalMilliseconds} ms");
                    break;
                }
            }
        }
    }
}
