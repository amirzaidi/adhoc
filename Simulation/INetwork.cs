using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Simulation
{
    interface INetwork<T>
    {
        public Task StartTransmission(INode<T> FromNode, T Packet, int Length, CancellationToken Token);

        public Vector3D GetNodePosition(INode<T> Node);

        public (List<INode<T>>, List<INode<T>>) SetNodePosition(INode<T> Node, Vector3D Point);

        public void ClearNodes();
    }
}
