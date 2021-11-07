using AdHocMAC.Nodes.MAC.Backoff;
using AdHocMAC.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes.MAC
{
    /// <summary>
    /// Contains the algorithm to handle back-off timers.
    /// This class is not thread-safe and should only be called from a single thread.
    /// </summary>
    class CollisionAvoidance
    {
        private const bool DEBUG = false;

        // To-Do: Fix the inefficiency of doing list iteration on every packet.
        private readonly List<(int, int)> mReceivedPacketIds = new List<(int, int)>();
        private readonly Dictionary<(int, int), CancellationTokenSource> mRunningTimers = new Dictionary<(int, int), CancellationTokenSource>();
        private readonly Random mRNG;

        private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(1);
        private readonly IBackoff mBackoff;

        public CollisionAvoidance(Random RNG)
        {
            mRNG = RNG;
            mBackoff = Configuration.CreateBackoff();
        }

        // Used to prevent reprocessing duplicate packets.
        public bool TryAddUniquePacketId(int FromNodeId, int Seq)
        {
            var tuple = (FromNodeId, Seq);
            if (mReceivedPacketIds.Contains(tuple))
            {
                return false;
            }

            mReceivedPacketIds.Add(tuple);
            return true;
        }

        public void CreatePacketTimer(int To, int Seq)
        {
            mRunningTimers[(To, Seq)] = new CancellationTokenSource();
        }

        // Used to time sending back ACKs.
        public async Task WaitPacketTimer(int To, int Seq, Action OnTimeout, Func<bool> IsChannelBusy, CancellationToken Token)
        {
            var tuple = (To, Seq);
            if (mRunningTimers.TryGetValue(tuple, out var CTS))
            {
                var CT = CancellationTokenSource.CreateLinkedTokenSource(CTS.Token, Token).Token;
                await Task.Delay((int)Math.Ceiling(Configuration.SIFS_SECONDS * 1000.0), CT).IgnoreExceptions();

                var slots = mRNG.Next(0, mBackoff.UpperBoundExcl()); // Add 1 timeslot to account for propagation delays.
                while (slots > 0 && !CT.IsCancellationRequested)
                {
                    await SyncedSlots.WaitUntilSlot(0, CT);
                    if (!IsChannelBusy())
                    {
                        slots -= 1;
                    }
                }

                var prevTimeout = mBackoff.UpperBoundExcl();
                if (CT.IsCancellationRequested)
                {
                    await mSemaphore.WaitAsync();
                    mBackoff.Decrease();
                    mSemaphore.Release();
                }
                else
                {
                    await mSemaphore.WaitAsync();
                    mBackoff.Increase();
                    mSemaphore.Release();

                    OnTimeout();
                }
                var newTimeout = mBackoff.UpperBoundExcl();
                if (DEBUG) Debug.WriteLine($"BACKOFF TIME CHANGE [{prevTimeout} -> {newTimeout}]");
            }
        }

        public void CancelPacketTimer(int To, int Seq)
        {
            var tuple = (To, Seq);
            if (mRunningTimers.TryGetValue(tuple, out var CTS))
            {
                CTS.Cancel();
                mRunningTimers.Remove(tuple); // Automatically remove the timer when done.

                if (DEBUG) Debug.WriteLine($"Stopped ACK timer [{To}, {Seq}]");
            }
            else
            {
                if (DEBUG) Debug.WriteLine($"Failed to stop ACK timer [{To}, {Seq}]");
            }
        }

        public int BacklogCount()
        {
            return mRunningTimers.Count;
        }
    }
}