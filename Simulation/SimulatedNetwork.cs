using System;
using System.Collections.Generic;

namespace AdHocMAC.Simulation
{
    class SimulatedNetwork<T>
    {
        private readonly Dictionary<INode<T>, Point3D> mNodes = new Dictionary<INode<T>, Point3D>();

        /// <summary>
        /// Tells the medium that a node is at a given location.
        /// If the medium has not seen the node before, it will automatically register it and update it with events.
        /// </summary>
        public void SetNodeAt(INode<T> Node, Point3D Point)
        {
            mNodes[Node] = Point;
        }

        /// <summary>
        /// Removes a node entirely.
        /// </summary>
        public void UnregisterNode(INode<T> Node)
        {
            if (!mNodes.Remove(Node))
            {
                throw new ArgumentException($"Node to be unregistered does not exist: {Node.GetID()}");
            }
        }
    }
}
