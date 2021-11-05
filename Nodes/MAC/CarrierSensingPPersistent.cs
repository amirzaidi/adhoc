using AdHocMAC.Utility;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    class CarrierSensingPPersistent : CarrierSensing
    {
        private readonly Random mRNG;
        private readonly int mMinDelay, mMaxDelay;
        private readonly double mPPersistency;
        private readonly SemaphoreSlim mLock = new SemaphoreSlim(1);
        private Task mSend = Task.CompletedTask;

        public CarrierSensingPPersistent(double PPersistency, Random RNG, int MinDelay, int MaxDelay) : base()
        {
            mPPersistency = PPersistency;
            mRNG = RNG;
            mMinDelay = MinDelay;
            mMaxDelay = MaxDelay;
        }

        public override async Task Send(Packet OutgoingPacket, CancellationToken Token)
        {
            if (!mIsChannelBusy)
            {
                await mLock.WaitAsync();
                mSend = mSend.ContinueWith(async _ => await SendWhenChannelFree(OutgoingPacket, Token));
                mLock.Release();

            }
        }
        private async Task SendWhenChannelFree(Packet OutgoingPacket, CancellationToken Token)
        {
            var time = DateTime.Now;
            int attempt = 0;

            while (!Token.IsCancellationRequested)
            {
                if (mIsChannelBusy)
                {
                    Debug.WriteLine($"[{OutgoingPacket.From}] LineBusy at Attempt {attempt} to send");
                    attempt++;
                }
                else
                {
                    if (mRNG.Next(0, 100) < (int)mPPersistency)
                    {
                        await SendAction(OutgoingPacket, Token);
                        Debug.WriteLine($"[{OutgoingPacket.From}] P NonBusy Sent After: {(int)(DateTime.Now - time).TotalMilliseconds} ms");
                        break;
                    }
                    else
                    {
                        Debug.WriteLine($"[{OutgoingPacket.From}] 1-P NonBusy NonSent Retrying");
                        Debug.WriteLine($"[{OutgoingPacket.From}] Awaiting random");
                        var sleeper = Task.Delay(mRNG.Next(mMinDelay, mMaxDelay), Token).IgnoreExceptions();
                        await sleeper;
                    }
                }
            }
        }
    }
}
