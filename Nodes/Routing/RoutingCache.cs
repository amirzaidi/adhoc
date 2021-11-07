using System.Collections.Generic;

namespace AdHocMAC.Nodes.Routing
{
    class RoutingCache
    {
        private readonly Dictionary<(int, int), int> mLastBroadcastSeq = new Dictionary<(int, int), int>();

        public bool TryUpdateRREQLastBroadcastSeq(int From, int To, int NewSeq)
        {
            var tuple = (From, To);
            if (mLastBroadcastSeq.TryGetValue(tuple, out int OldSeq) && OldSeq == NewSeq)
            {
                return false;
            }

            mLastBroadcastSeq[tuple] = NewSeq;
            return true;
        }
    }
}
