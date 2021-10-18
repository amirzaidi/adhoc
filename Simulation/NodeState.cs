using System.Collections.Generic;
using System.Threading;

namespace AdHocMAC.Simulation
{
    class NodeState<T>
    {
        public Point3D Position;
        public CancellationTokenSource PositionChangeCTS;

        public int OngoingTransmissions;
        public bool HasCollided;
    }
}
