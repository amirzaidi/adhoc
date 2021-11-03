using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Simulation
{
    class SimulatedNetwork<T>
    {
        private enum TransmissionState
        {
            NotArrived,
            Ongoing,
            Interrupted
        }

        private readonly Dictionary<INode<T>, NodeState<T>> mNodes = new Dictionary<INode<T>, NodeState<T>>();

        private double mRange = 200.0; // Range in Point3D euclidian units.
        private double mTransmittedUnitsPerSecond = 32.0; // Characters sent per second.
        private double mTravelDistancePerSecond = 50.0; // Speed of light in this system.

        public void StartTransmission(INode<T> FromNode, T OutgoingPacket, int Length)
        {
            var signalDuration = Length / mTransmittedUnitsPerSecond;
            CancellationTokenSource CTSOutgoing() => mNodes[FromNode].PositionChangeCTS;

            // Start a stopwatch for accurate time measurement.
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            foreach (var KVP in mNodes)
            {
                if (Equals(FromNode, KVP.Key))
                {
                    continue;
                }

                var KVPCopy = KVP; // Ensure we have an in-memory representation.
                // "Start" a new thread to handle each point to point transmission.
                Task.Run(async () =>
                {
                    var state = TransmissionState.NotArrived;
                    var packetIfSuccessful = OutgoingPacket; // We set this to null when it should not be received.

                    CancellationTokenSource CTSIncoming() => KVP.Value.PositionChangeCTS;

                    while (true)
                    {
                        var CTS = CancellationTokenSource.CreateLinkedTokenSource(CTSOutgoing().Token, CTSIncoming().Token);
                        var distance = Point3D.Distance(mNodes[FromNode].Position, KVPCopy.Value.Position);
                        var inRange = distance <= mRange;

                        var signalStartTime = distance / mTravelDistancePerSecond;
                        var signalEndTime = signalStartTime + signalDuration;

                        Debug.WriteLine($"{KVPCopy.Key.GetID()}: Send Times [{signalStartTime}, {signalEndTime}]");

                        switch (state)
                        {
                            // State 1: The packet has not reached the destination.
                            case TransmissionState.NotArrived:
                                await WaitUntil(stopWatch, signalStartTime, CTS.Token);
                                if (!CTS.Token.IsCancellationRequested)
                                {
                                    if (inRange)
                                    {
                                        IncreaseTransmissions(KVPCopy);
                                        state = TransmissionState.Ongoing;
                                        Debug.WriteLine($"{KVPCopy.Key.GetID()}: NotArrived > Ongoing");
                                    }
                                    else
                                    {
                                        state = TransmissionState.Interrupted;
                                        Debug.WriteLine($"{KVPCopy.Key.GetID()}: NotArrived > Interrupted");
                                    }
                                }
                                break;
                            // State 2: The packet has started reaching the destination with enough signal strength.
                            case TransmissionState.Ongoing:
                                if (!inRange) // Out of range, switch to Interrupted and destroy the packet.
                                {
                                    state = TransmissionState.Interrupted;
                                    packetIfSuccessful = default; // We set this to null to indicate it was lost.
                                    DecreaseTransmissions(KVPCopy, packetIfSuccessful);
                                    Debug.WriteLine($"{KVPCopy.Key.GetID()}: Ongoing > Interrupted");
                                    break;
                                }

                                await WaitUntil(stopWatch, signalEndTime, CTS.Token);
                                if (!CTS.Token.IsCancellationRequested)
                                {
                                    DecreaseTransmissions(KVPCopy, packetIfSuccessful);
                                    Debug.WriteLine($"{KVPCopy.Key.GetID()}: Ongoing > Exit");
                                    return; // This is one of two exit conditions.
                                }
                                break;
                            // State 3: The packet has started reaching the destination, but the signal strength is too low.
                            case TransmissionState.Interrupted:
                                if (inRange) // In range, switch back to Ongoing, but the packet is still lost.
                                {
                                    state = TransmissionState.Ongoing;
                                    IncreaseTransmissions(KVPCopy);
                                    Debug.WriteLine($"{KVPCopy.Key.GetID()}: Interrupted > Ongoing");
                                    break;
                                }

                                await WaitUntil(stopWatch, signalEndTime, CTS.Token);
                                if (!CTS.Token.IsCancellationRequested)
                                {
                                    Debug.WriteLine($"{KVPCopy.Key.GetID()}: Interrupted > Exit");
                                    return; // This is one of two exit conditions.
                                }
                                break;
                        }
                    }
                });
            }
        }

        private async Task WaitUntil(Stopwatch SW, double UntilTime, CancellationToken Token)
        {
            var timeToEventMS = (int)Math.Floor(1000.0 * UntilTime - SW.ElapsedMilliseconds);
            if (timeToEventMS > 0)
            {
                await Task.Delay(timeToEventMS, Token); // First, wait the milliseconds part async.
            }

            while (SW.ElapsedTicks < (long)(UntilTime * Stopwatch.Frequency) && !Token.IsCancellationRequested)
            {
                // Burn CPU until the right timing.
            }
        }

        private void IncreaseTransmissions(KeyValuePair<INode<T>, NodeState<T>> KVP)
        {
            // Started transmission.
            lock (KVP.Key) // Lock for thread-safety.
            {
                if (++KVP.Value.OngoingTransmissions == 1)
                {
                    KVP.Key.OnReceiveStart();
                }
                else
                {
                    KVP.Key.OnReceiveCollide();
                    KVP.Value.HasCollided = true; // Set this flag for the last transmission to see.
                }
            }
        }

        private void DecreaseTransmissions(KeyValuePair<INode<T>, NodeState<T>> KVP, T PacketIfSuccessful)
        {
            // Completed transmission.
            lock (KVP.Key) // Lock for thread-safety.
            {
                if (--KVP.Value.OngoingTransmissions == 0)
                {
                    if (!KVP.Value.HasCollided && PacketIfSuccessful != null)
                    {
                        Task.Run(() => KVP.Key.OnReceiveSuccess(PacketIfSuccessful));
                    }

                    KVP.Key.OnReceiveEnd();
                    KVP.Value.HasCollided = false; // Reset this.
                }
            }
        }

        // Reuse these two variables for efficiency.
        private readonly List<INode<T>> mConnectedNodes = new List<INode<T>>();
        private readonly List<INode<T>> mDisconnectedNodes = new List<INode<T>>();

        /// <summary>
        /// Tells the medium that a node is at a given location.
        /// If the medium has not seen the node before, it will automatically register it.
        /// </summary>
        public (List<INode<T>>, List<INode<T>>) SetNodeAt(INode<T> Node, Point3D Point)
        {
            mConnectedNodes.Clear();
            mDisconnectedNodes.Clear();

            if (mNodes.TryGetValue(Node, out var nodeState))
            {
                // Cancel all timers relating to the old position of the node.
                nodeState.PositionChangeCTS.Cancel();

                foreach (var node in mNodes)
                {
                    if (!Equals(node, Node))
                    {
                        var wasInRange = Point3D.Distance(nodeState.Position, node.Value.Position) <= mRange;
                        var isInRange = Point3D.Distance(Point, node.Value.Position) <= mRange;

                        if (isInRange && !wasInRange)
                        {
                            mConnectedNodes.Add(node.Key);
                        }
                        else if (!isInRange && wasInRange)
                        {
                            mDisconnectedNodes.Add(node.Key);
                        }
                    }
                }
            }
            else
            {
                // This node is new, add it.
                foreach (var node in mNodes)
                {
                    if (Point3D.Distance(Point, node.Value.Position) <= mRange)
                    {
                        mConnectedNodes.Add(node.Key);
                    }
                }

                nodeState = new NodeState<T>();
                mNodes.Add(Node, nodeState);
            }

            // Update the new position.
            nodeState.Position = Point;
            nodeState.PositionChangeCTS = new CancellationTokenSource();

            return (mConnectedNodes, mDisconnectedNodes);
        }

        /// <summary>
        /// Removes a node entirely.
        /// </summary>
        public void UnregisterNode(INode<T> Node)
        {
            if (!mNodes.Remove(Node))
            {
                throw new ArgumentException($"Node to be unregistered does not exist");
            }
        }
        
        public void ClearNodes()
        {
            mNodes.Clear();
        }
    }
}
