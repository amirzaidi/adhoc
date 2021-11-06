using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AdHocMAC.Simulation
{
    class NodeState<T>
    {
        public Vector3D Position;
        public CancellationTokenSource PositionChangeCTS;

        public List<(int, T)> OngoingTransmissions = new List<(int, T)>();
        public bool HasCollided;

        public (List<int>, List<int>) AddTransmission(int FromNodeId, T Packet)
        {
            var prevTransmissions = UniqueOngoingTransmission();
            OngoingTransmissions.Add((FromNodeId, Packet));
            var newTransmissions = UniqueOngoingTransmission();
            return (prevTransmissions, newTransmissions);
        }

        public (List<int>, List<int>) RemoveTransmission(int FromNodeId, T Packet)
        {
            var prevTransmissions = UniqueOngoingTransmission();
            OngoingTransmissions.Remove((FromNodeId, Packet));
            var newTransmissions = UniqueOngoingTransmission();
            return (prevTransmissions, newTransmissions);
        }

        private List<int> UniqueOngoingTransmission() => OngoingTransmissions.Select(x => x.Item1).Distinct().ToList();
    }
}
