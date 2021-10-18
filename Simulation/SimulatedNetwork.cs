using System;
using System.Collections.Generic;

namespace AdHocMAC.Simulation
{
    class SimulatedNetwork<T> : IMedium<T>
    {
        private readonly Dictionary<INode<T>, Point3D> mNodes = new Dictionary<INode<T>, Point3D>();

        public void SetNodeAt(INode<T> Node, Point3D Point)
        {
            mNodes[Node] = Point;
        }

        public void UnregisterNode(INode<T> Node)
        {
            if (!mNodes.Remove(Node))
            {
                throw new ArgumentException($"Node to be unregistered does not exist: {Node.GetID()}");
            }
        }
    }
}
