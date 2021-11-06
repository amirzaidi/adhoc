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
        private const bool DEBUG = false;

        private enum TransmissionState
        {
            NotArrived,
            Ongoing,
            Interrupted
        }

        private readonly Dictionary<INode<T>, NodeState<T>> mNodes = new Dictionary<INode<T>, NodeState<T>>();
        private readonly INetworkEventLogger<INode<T>> mLogger;

        private readonly double mRange; // Range in Point3D euclidian units.

        private double mTransmittedUnitsPerSecond = 256.0; // Characters sent per second.
        private double mTravelDistancePerSecond = 2048.0; // Speed of light in this system.

        public PhysicsNetwork(INetworkEventLogger<INode<T>> Logger, double Range)
        {
            mLogger = Logger;
            mRange = Range;
        }

        public async Task StartTransmission(INode<T> FromNode, T Packet, int Length, CancellationToken Token)
        {
            if (!mNodes.TryGetValue(FromNode, out var fromNodeState))
            {
                return;
            }

            CancellationToken CTOutgoing() => fromNodeState.PositionChangeCTS.Token;

            var signalDuration = Length / mTransmittedUnitsPerSecond;

            // Start a stopwatch for accurate time measurement.
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // We increase the transmission count to ensure no data can be received.
            mLogger.BeginSend(FromNode);
            AddTransmission(FromNode, FromNode, fromNodeState, Packet);

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
                _ = Task.Run(async () => await TransmitOverLink(
                    FromNode, fromNodeState, toNode, toNodeState,
                    stopwatch, signalDuration, Packet, CTOutgoing, Token
                ));
            }

            // Time until the sender is done transmitting.
            await WaitUntil(stopwatch, signalDuration, Token);

            // We can start seeing incoming data now.
            mLogger.EndSend(FromNode);
            RemoveTransmission(FromNode, FromNode, fromNodeState, Packet, false);
        }

        private async Task TransmitOverLink(INode<T> FromNode, NodeState<T> FromNodeState, INode<T> ToNode, NodeState<T> ToNodeState,
            Stopwatch SW, double SignalDuration, T Packet, Func<CancellationToken> CTOutgoing, CancellationToken Token)
        {
            var state = TransmissionState.NotArrived;
            var packetValid = true;

            var debugPrint = $"{FromNode.GetID()} -> {ToNode.GetID()}";

            CancellationToken CTIncoming() => CancellationTokenSource.CreateLinkedTokenSource(ToNodeState.PositionChangeCTS.Token, Token).Token;
            while (!Token.IsCancellationRequested)
            {
                var linkToken = CancellationTokenSource.CreateLinkedTokenSource(CTOutgoing(), CTIncoming()).Token;
                var distance = Vector3D.Distance(FromNodeState.Position, ToNodeState.Position);
                var inRange = distance <= mRange;

                var signalStartTime = distance / mTravelDistancePerSecond;
                var signalEndTime = signalStartTime + SignalDuration;

                if (DEBUG) Debug.WriteLine($"{debugPrint}: Send Times [{signalStartTime}, {signalEndTime}]");

                switch (state)
                {
                    // State 1: The packet has not reached the destination.
                    case TransmissionState.NotArrived:
                        await WaitUntil(SW, signalStartTime, linkToken);
                        if (!linkToken.IsCancellationRequested)
                        {
                            if (inRange)
                            {
                                AddTransmission(FromNode, ToNode, ToNodeState, Packet);
                                state = TransmissionState.Ongoing;

                                if (DEBUG) Debug.WriteLine($"{debugPrint}: NotArrived > Ongoing");
                                mLogger.BeginReceive(ToNode, FromNode); // BeginReceive when we enter Ongoing.
                            }
                            else
                            {
                                state = TransmissionState.Interrupted;
                                if (DEBUG) Debug.WriteLine($"{debugPrint}: NotArrived > Interrupted");
                            }
                        }
                        break;
                    // State 2: The packet has started reaching the destination with enough signal strength.
                    case TransmissionState.Ongoing:
                        if (!inRange) // Out of range, switch to Interrupted and destroy the packet.
                        {
                            state = TransmissionState.Interrupted;
                            packetValid = false;
                            RemoveTransmission(FromNode, ToNode, ToNodeState, Packet, packetValid);

                            if (DEBUG) Debug.WriteLine($"{debugPrint}: Ongoing > Interrupted");
                            mLogger.EndReceive(ToNode, FromNode); // EndReceive when we leave Ongoing.

                            break;
                        }

                        await WaitUntil(SW, signalEndTime, linkToken);
                        if (!linkToken.IsCancellationRequested)
                        {
                            RemoveTransmission(FromNode, ToNode, ToNodeState, Packet, packetValid);

                            if (DEBUG) Debug.WriteLine($"{debugPrint}: Ongoing > Exit");
                            mLogger.EndReceive(ToNode, FromNode); // EndReceive when we leave Ongoing.

                            return; // This is one of two exit conditions.
                        }
                        break;
                    // State 3: The packet has started reaching the destination, but the signal strength is too low.
                    case TransmissionState.Interrupted:
                        if (inRange) // In range, switch back to Ongoing, but the packet is still lost.
                        {
                            state = TransmissionState.Ongoing;
                            AddTransmission(FromNode, ToNode, ToNodeState, Packet);

                            if (DEBUG) Debug.WriteLine($"{debugPrint}: Interrupted > Ongoing");
                            mLogger.BeginReceive(ToNode, FromNode); // BeginReceive when we enter Ongoing.

                            break;
                        }

                        await WaitUntil(SW, signalEndTime, linkToken);
                        if (!linkToken.IsCancellationRequested)
                        {
                            if (DEBUG) Debug.WriteLine($"{debugPrint}: Interrupted > Exit");
                            return; // This is one of two exit conditions.
                        }
                        break;
                }
            }
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

        private void AddTransmission(INode<T> FromNode, INode<T> ToNode, NodeState<T> ToNodeState, T Packet)
        {
            // Started transmission.
            lock (ToNodeState) // Lock for thread-safety.
            {
                var (prevTransmissions, newTransmissions) = ToNodeState.AddTransmission(FromNode.GetID(), Packet);

                // If this link was already active, ignore this call to AddTransmission.
                if (!prevTransmissions.Contains(FromNode.GetID()))
                {
                    if (newTransmissions.Count == 1)
                    {
                        ToNode.OnReceiveStart();
                    }
                    else
                    {
                        ToNode.OnReceiveCollide();
                        ToNodeState.HasCollided = true; // Set this flag for the last transmission to see.
                    }
                }
            }
        }

        private void RemoveTransmission(INode<T> FromNode, INode<T> ToNode, NodeState<T> ToNodeState, T Packet, bool PacketValid)
        {
            // Completed transmission.
            lock (ToNodeState) // Lock for thread-safety.
            {
                var (_, newTransmissions) = ToNodeState.RemoveTransmission(FromNode.GetID(), Packet);
                var deliverPacket = !ToNodeState.HasCollided && PacketValid;

                // We can deliver some packets even when this is not the case, in back-to-back packets.
                if (newTransmissions.Count == 0)
                {
                    ToNode.OnReceiveEnd();
                    ToNodeState.HasCollided = false; // Reset this.
                }

                // Call OnReceiveSuccess after OnReceiveEnd, so that the channel might be already free.
                if (deliverPacket)
                {
                    ToNode.OnReceiveSuccess(Packet);
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
                    if (!Equals(node.Key, Node))
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
