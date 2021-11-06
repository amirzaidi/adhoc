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

        private const int MIN_TIMEOUT_MS = 1000;
        private const int MAX_TIMEOUT_MS = 8 * MIN_TIMEOUT_MS;

        // To-Do: Fix the inefficiency of doing list iteration on every packet.
        private readonly List<(int, int)> mReceivedPacketIds = new List<(int, int)>();
        private readonly Dictionary<(int, int), CancellationTokenSource> mRunningTimers = new Dictionary<(int, int), CancellationTokenSource>();

        private int mTimeout = MIN_TIMEOUT_MS;

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
        public async Task WaitPacketTimer(int To, int Seq, Action OnTimeout, CancellationToken Token)
        {
            var tuple = (To, Seq);
            if (mRunningTimers.TryGetValue(tuple, out var CTS))
            {
                var CT = CancellationTokenSource.CreateLinkedTokenSource(CTS.Token, Token).Token;
                await Task.Delay(mTimeout, CT).IgnoreExceptions();

                if (CT.IsCancellationRequested)
                {
                    // To-Do: Find out better BEB algorithm without lock.
                    lock (this)
                    {
                        mTimeout = MIN_TIMEOUT_MS;
                    }
                }
                else
                {
                    lock (this)
                    {
                        if (mTimeout < MAX_TIMEOUT_MS)
                        {
                            mTimeout *= 2;
                        }
                    }

                    OnTimeout();
                }
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