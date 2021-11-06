using AdHocMAC.GUI;
using AdHocMAC.Nodes;
using AdHocMAC.Nodes.MAC;
using AdHocMAC.Nodes.Routing;
using AdHocMAC.Simulation;
using AdHocMAC.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AdHocMAC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int TOP_LEVEL_SEED = 1234567890;
        private readonly Random mSeedGenerator = new Random(TOP_LEVEL_SEED);

        // Fields to handle showing what is happening to the user.
        private readonly LogHandler mLogHandler = new LogHandler();
        private readonly NodeVisualizer<INode<Packet>> mNodeVisualizer;

        // The real-time network simulation.
        private readonly INetwork<Packet> mNetwork;

        // Prevent doing multiple Resets simultaneously.
        private readonly DuplicateRunDiscarder mReset;

        // We use these two to stop all running nodes at the same time.
        private CancellationTokenSource mCTS;
        private readonly List<Node> mNodes = new List<Node>();
        private readonly List<Task> mNodeThreads = new List<Task>();

        // Determined by UI slider.
        private int mNodeCount;

        private int mMinDelay = 100;
        private int mMaxDelay = 500;
        private double mPPersistency = 0.4;
        private double mRange = 200.0;

        public MainWindow()
        {
            InitializeComponent();
            mNodeVisualizer = new NodeVisualizer<INode<Packet>>(
                this,
                Grid.Children,
                (n1, x, y) =>
                {
                    var (added, removed) = mNetwork.SetNodePosition(n1, Vector3D.Create(x, y));
                    added.ForEach(n2 => mNodeVisualizer.ConnectNodes(n1, n2));
                    removed.ForEach(n2 => mNodeVisualizer.DisconnectNodes(n1, n2));
                },
                n => n.GetID()
            );

            var nodeVisualizerEvents = new NodeVisualizerEvents<INode<Packet>>(mNodeVisualizer);

            mNetwork = new PhysicsNetwork<Packet>(nodeVisualizerEvents, mRange);
            mReset = new DuplicateRunDiscarder(Reset);
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            await mReset.Execute();
        }

        private async Task Reset()
        {
            if (mCTS != null)
            {
                mLogHandler.OnDebug($"Stopping {mNodeThreads.Count} nodes");
                mCTS.Cancel();

                foreach (var nodeThread in mNodeThreads)
                {
                    await nodeThread;
                }

                foreach (var node in mNodes)
                {
                    node.GetLog().Clear();
                }
            }

            mNodeThreads.Clear();
            mNetwork.ClearNodes();

            mNodeCount = (int) NodeCount.Value;
            mLogHandler.OnDebug($"Creating {mNodeCount} new nodes");

            var nodes = new List<Node>();

            mCTS = new CancellationTokenSource();
            for (int i = 0; i < mNodeCount; i++)
            {
                /*
                // To-Do: Make the outgoing packet function use the SimulatedNetwork instead of only logging.
                var protocol = new CarrierSensingPPersistent
                (
                    // This should become a lambda that directly transmits on the network (rather than logging).
                    Packet => mLogHandler.OnDebug(i, $"Sending: {Packet.Data} to {Packet.To}"),
                    mPPersistency
                );
                */

                //var protocol = new Aloha();
                //var protocol = new CarrierSensingNonPersistent(new Random(mSeedGenerator.Next()), mMinDelay, mMaxDelay);
                var protocol = new CarrierSensingPPersistent(new Random(mSeedGenerator.Next()), mPPersistency);
                var node = new Node(i, mNodeCount, protocol, new Random(mSeedGenerator.Next()));

                // We set this afterwards because we need a reference to node.
                protocol.SendAction = async (p, ct) => await mNetwork.StartTransmission(node, p, Packet.GetLength(p), ct);

                // Add newly created node.
                nodes.Add(node);
            }

            // Show everything in the UI.
            mNodeVisualizer.ResetNodes(new List<INode<Packet>>(nodes));

            // Start all logic loops when they are added to the UI.
            foreach (var node in nodes)
            {
                // Use Task.Run to force each loop onto a separate execution context.
                mNodeThreads.Add(Task.Run(() => node.Loop(mCTS.Token)));
            }

            // To-Do: Remove double list.
            mNodes.Clear();
            mNodes.AddRange(nodes);

            mLogHandler.OnDebug($"Started {mNodeThreads.Count} nodes");
        }

        private void Route_Click(object sender, RoutedEventArgs e)
        {
            Routing.GetShortestPath(mNodes, n => mNetwork.GetNodePosition(n), mRange);
        }

        private async void Log_Click(object sender, RoutedEventArgs e)
        {
            var lines = new List<string>
            {
                "Type, SenderID, ReceiverID, SeqNum, AttemptNumber, TimeInitialSend, TimeSentOrReceived"
            };

            foreach (var node in mNodes)
            {
                lines.AddRange(node.GetLog());
            }

            await File.WriteAllLinesAsync($"log-{DateTime.Now.ToString().Replace("\\", "-").Replace(":", "-")}.txt", lines);
        }
    }
}
