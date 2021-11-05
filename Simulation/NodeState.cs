using System.Threading;

namespace AdHocMAC.Simulation
{
    class NodeState<T>
    {
        public Vector3D Position;
        public CancellationTokenSource PositionChangeCTS;

        public int OngoingTransmissions;
        public bool HasCollided;
    }
}
