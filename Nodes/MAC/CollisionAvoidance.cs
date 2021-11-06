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

        private const int TIMEOUT_MS = 2500;

        // To-Do: Fix the inefficiency of doing list iteration on every packet.
        private List<(int, int)> mReceivedPacketIds = new List<(int, int)>();
        private Dictionary<(int, int), CancellationTokenSource> mRunningTimers = new Dictionary<(int, int), CancellationTokenSource>();

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

        // Used to time sending back ACKs.
        public void StartPacketTimer(int To, int Seq, Action OnTimeout, CancellationToken Token)
        {
            var CTS = new CancellationTokenSource();
            var tuple = (To, Seq);
            mRunningTimers[tuple] = CTS; // Can overwrite previous timer.

            Task.Run(async () =>
            {
                var CT = CancellationTokenSource.CreateLinkedTokenSource(CTS.Token, Token).Token;
                await Task.Delay(TIMEOUT_MS, CT).IgnoreExceptions();
                mRunningTimers.Remove(tuple); // Automatically remove the timer when done.

                if (!CT.IsCancellationRequested)
                {
                    OnTimeout();
                }
            });
        }

        public void StopPacketTimer(int To, int Seq)
        {
            if (mRunningTimers.TryGetValue((To, Seq), out var CTS))
            {
                CTS.Cancel();
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