using AdHocMAC.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AdHocMAC.Simulation
{
    class PhysicsNetwork<T> : INetwork<T>
    {
        private enum TransmissionState
        {
            NotArrived,
            Ongoing,
            Interrupted
        }

        private readonly Dictionary<INode<T>, NodeState<T>> mNodes = new Dictionary<INode<T>, NodeState<T>>();
        private readonly INetworkEventLogger<INode<T>> mLogger;

        private readonly double mRange; // Range in Point3D euclidian units.

        private double mTransmittedUnitsPerSecond = 16.0; // Characters sent per second.
        private double mTravelDistancePerSecond = 256.0; // Speed of light in this system.

        public PhysicsNetwork(INetworkEventLogger<INode<T>> Logger, double Range)
        {
            mLogger = Logger;
            mRange = Range;
        }

        public async Task StartTransmission(INode<T> FromNode, T OutgoingPacket, int Length, CancellationToken Token)
        {
            if (!mNodes.TryGetValue(FromNode, out var fromNodeState))
            {
                return;
            }

            CancellationToken CTOutgoing() =>
                CancellationTokenSource.CreateLinkedTokenSource(fromNodeState.PositionChangeCTS.Token, Token).Token;

            var signalDuration = Length / mTransmittedUnitsPerSecond;

            // Start a stopwatch for accurate time measurement.
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // We increase the transmission count to ensure no data can be received.
            mLogger.BeginSend(FromNode);
            IncreaseTransmissions(FromNode, fromNodeState);

            foreach (var KVP in mNodes)
            {
                // Ensure we have an in-memory representation.
                var toNode = KVP.Key;
                var toNodeState = KVP.Value;

                if (Equals(FromNode, toNode))
                {
                    // Skip the simulation of sending to itself.
                    continue;
                }

                // "Start" a new thread to handle each point to point transmission.
                _ = Task.Run(async () =>
                {
                    var state = TransmissionState.NotArrived;
                    var shouldDeliverPacket = true;

                    var debugPrint = $"{FromNode.GetID()} -> {toNode.GetID()}";

                    CancellationToken CTIncoming() => toNodeState.PositionChangeCTS.Token;

                    while (!Token.IsCancellationRequested)
                    {
                        var linkToken = CancellationTokenSource.CreateLinkedTokenSource(CTOutgoing(), CTIncoming()).Token;
                        var distance = Vector3D.Distance(fromNodeState.Position, toNodeState.Position);
                        var inRange = distance <= mRange;

                        var signalStartTime = distance / mTravelDistancePerSecond;
                        var signalEndTime = signalStartTime + signalDuration;

                        Debug.WriteLine($"{debugPrint}: Send Times [{signalStartTime}, {signalEndTime}]");

                        switch (state)
                        {
                            // State 1: The packet has not reached the destination.
                            case TransmissionState.NotArrived:
                                await WaitUntil(stopWatch, signalStartTime, linkToken);
                                if (!linkToken.IsCancellationRequested)
                                {
                                    if (inRange)
                                    {
                                        IncreaseTransmissions(toNode, toNodeState);
                                        state = TransmissionState.Ongoing;

                                        Debug.WriteLine($"{debugPrint}: NotArrived > Ongoing");
                                        mLogger.BeginReceive(toNode, FromNode); // BeginReceive when we enter Ongoing.
                                    }
                                    else
                                    {
                                        state = TransmissionState.Interrupted;
                                        Debug.WriteLine($"{debugPrint}: NotArrived > Interrupted");
                                    }
                                }
                                break;
                            // State 2: The packet has started reaching the destination with enough signal strength.
                            case TransmissionState.Ongoing:
                                if (!inRange) // Out of range, switch to Interrupted and destroy the packet.
                                {
                                    state = TransmissionState.Interrupted;
                                    shouldDeliverPacket = false;
                                    DecreaseTransmissions(toNode, toNodeState, shouldDeliverPacket, OutgoingPacket);

                                    Debug.WriteLine($"{debugPrint}: Ongoing > Interrupted");
                                    mLogger.EndReceive(toNode, FromNode); // EndReceive when we leave Ongoing.

                                    break;
                                }

                                await WaitUntil(stopWatch, signalEndTime, linkToken);
                                if (!linkToken.IsCancellationRequested)
                                {
                                    DecreaseTransmissions(toNode, toNodeState, shouldDeliverPacket, OutgoingPacket);

                                    Debug.WriteLine($"{debugPrint}: Ongoing > Exit");
                                    mLogger.EndReceive(toNode, FromNode); // EndReceive when we leave Ongoing.

                                    return; // This is one of two exit conditions.
                                }
                                break;
                            // State 3: The packet has started reaching the destination, but the signal strength is too low.
                            case TransmissionState.Interrupted:
                                if (inRange) // In range, switch back to Ongoing, but the packet is still lost.
                                {
                                    state = TransmissionState.Ongoing;
                                    IncreaseTransmissions(toNode, toNodeState);

                                    Debug.WriteLine($"{debugPrint}: Interrupted > Ongoing");
                                    mLogger.BeginReceive(toNode, FromNode); // BeginReceive when we enter Ongoing.

                                    break;
                                }

                                await WaitUntil(stopWatch, signalEndTime, linkToken);
                                if (!linkToken.IsCancellationRequested)
                                {
                                    Debug.WriteLine($"{debugPrint}: Interrupted > Exit");
                                    return; // This is one of two exit conditions.
                                }
                                break;
                        }
                    }
                });
            }

            // Time until the sender is done transmitting.
            await WaitUntil(stopWatch, signalDuration, Token);

            // We can start seeing incoming data now.
            mLogger.EndSend(FromNode);
            DecreaseTransmissions(FromNode, fromNodeState, false, default);
        }

        private async Task WaitUntil(Stopwatch SW, double UntilTime, CancellationToken Token)
        {
            var timeToEventMS = (int)Math.Floor(1000.0 * UntilTime - SW.ElapsedMilliseconds);
            if (timeToEventMS > 0)
            {
                await Task.Delay(timeToEventMS, Token).IgnoreExceptions(); // First, wait the milliseconds part async.
            }

            while (SW.ElapsedTicks < (long)(UntilTime * Stopwatch.Frequency) && !Token.IsCancellationRequested)
            {
                // Burn CPU until the right timing.
            }
        }

        private void IncreaseTransmissions(INode<T> Node, NodeState<T> NodeState)
        {
            // Started transmission.
            lock (NodeState) // Lock for thread-safety.
            {
                if (++NodeState.OngoingTransmissions == 1)
                {
                    Node.OnReceiveStart();
                }
                else
                {
                    Node.OnReceiveCollide();
                    NodeState.HasCollided = true; // Set this flag for the last transmission to see.
                }
            }
        }

        private void DecreaseTransmissions(INode<T> Node, NodeState<T> NodeState, bool ShouldDeliverPacket, T Packet)
        {
            // Completed transmission.
            lock (NodeState) // Lock for thread-safety.
            {
                if (--NodeState.OngoingTransmissions == 0)
                {
                    if (!NodeState.HasCollided && ShouldDeliverPacket)
                    {
                        Node.OnReceiveSuccess(Packet);
                    }

                    Node.OnReceiveEnd();
                    NodeState.HasCollided = false; // Reset this.
                }
            }
        }

        public Vector3D GetNodePosition(INode<T> Node)
        {
            return mNodes[Node].Position;
        }

        // Reuse these two variables for efficiency.
        private readonly List<INode<T>> mConnectedNodes = new List<INode<T>>();
        private readonly List<INode<T>> mDisconnectedNodes = new List<INode<T>>();

        /// <summary>
        /// Tells the medium that a node is at a given location.
        /// If the medium has not seen the node before, it will automatically register it.
        /// </summary>
        public (List<INode<T>>, List<INode<T>>) SetNodePosition(INode<T> Node, Vector3D Point)
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
                        var wasInRange = Vector3D.Distance(nodeState.Position, node.Value.Position) <= mRange;
                        var isInRange = Vector3D.Distance(Point, node.Value.Position) <= mRange;

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
                    if (Vector3D.Distance(Point, node.Value.Position) <= mRange)
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

        public void ClearNodes()
        {
            mNodes.Clear();
        }
    }
}
