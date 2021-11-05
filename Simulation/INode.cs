namespace AdHocMAC.Simulation
{
    /// <summary>
    /// None of these callbacks should do heavy work, as the network waits for completion.
    /// </summary>
    /// <typeparam name="T">Type of packet that is transmitted.</typeparam>
    interface INode<T>
    {
        public int GetID();

        /// <summary>
        /// Sense the start of an incoming packet.
        /// </summary>
        public void OnReceiveStart();

        /// <summary>
        /// Sense a collision of an incoming packet.
        /// </summary>
        public void OnReceiveCollide();

        /// <summary>
        /// There was no collision and the packet was received successfully.
        /// </summary>
        public void OnReceiveSuccess(T IncomingPacket);

        /// <summary>
        /// Always called at the end when the channel is free.
        /// </summary>
        public void OnReceiveEnd();
    }
}
