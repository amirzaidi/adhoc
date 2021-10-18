namespace AdHocMAC.Simulation
{
    interface IMedium<T>
    {
        /// <summary>
        /// Tells the medium that a node is at a given location.
        /// If the medium has not seen the node before, it will automatically register it and update it with events.
        /// </summary>
        public void SetNodeAt(INode<T> Node, Point3D Point);

        /// <summary>
        /// Removes a node from the medium.
        /// </summary>
        public void UnregisterNode(INode<T> Node);
    }
}
