using AdHocMAC.Simulation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Nodes
{
    /// <summary>
    /// This node thinks there is a real network layer attached to it.
    /// </summary>
    class Node : INode<Packet>
    {
        private readonly int mId;
        private readonly Action<Packet> mTransmit;

        private readonly SemaphoreSlim mShutdownWaiter = new SemaphoreSlim(0);
        private bool mRun = true;

        public Node(int Id, Action<Packet> Transmit)
        {
            mId = Id;
            mTransmit = Transmit;
        }

        public int GetID() => mId;

        // "Starts" the background thread of this node's logic.
        public void Start() => Task.Run(Loop);


        /// <summary>
        /// This can do background work, such as coming up with random packets to send.
        /// </summary>
        public async Task Loop()
        {
            while (mRun)
            {
                await Task.Delay(1000);

                // Send a Hello World packet to the node with ID+1.
                Send(new Packet
                {
                    From = GetID(),
                    To = GetID() + 1,
                    Data = "Hello World!"
                });
            }

            // The loop has successfully stopped.
            mShutdownWaiter.Release();
        }

        public async Task Stop()
        {
            // Tell the logic loop to stop.
            mRun = false;

            // Wait for the logic loop to actually stop.
            await mShutdownWaiter.WaitAsync();
        }

        private void Send(Packet OutgoingPacket)
        {
            // This is ALOHA. Should be replaced with CSMA/CA.
            mTransmit(OutgoingPacket);
        }

        /*
         * From here on, the events should use CSMA/CA to handle what's happening.
         */
        public void OnReceiveStart()
        {
        }

        public void OnReceiveCollide()
        {
            // This one is mutually exclusive with OnReceiveSuccess.
            // Either this event happens, or the other one, but not both.
        }

        public void OnReceiveSuccess(Packet IncomingPacket)
        {
        }

        public void OnReceiveEnd()
        {
        }
    }
}
