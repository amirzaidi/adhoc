using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Simulation
{
    class SimpleNetwork<T> : INetwork<T>
    {
        public async Task StartTransmission(INode<T> FromNode, T OutgoingPacket, int Length, CancellationToken Token)
        {
        }

        public Vector3D GetNodePosition(INode<T> Node)
        {
            return default(Vector3D);
        }

        public (List<INode<T>>, List<INode<T>>) SetNodePosition(INode<T> Node, Vector3D Point)
        {
            return (null, null);
        }

        public void ClearNodes()
        {
        }
    }
}
