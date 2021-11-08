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
using System.Globalization;
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

            mNetwork = new PhysicsNetwork<Packet>(nodeVisualizerEvents, Configuration.PHYSICS_RANGE);
            mReset = new DuplicateRunDiscarder(Reset);

            NodeCount.Value = Configuration.AUTO_RUN_NODE_COUNT;

            if (Configuration.AUTO_RUN_SHUT_DOWN_AFTER != -1)
            {
                SynchronizationContext.Current.Post(async _ =>
                {
                    await mReset.Execute();
                    if (!Configuration.AUTO_RUN_PACKETS_ENABLED)
                    {
                        mNodes[0].StartRouteRequest(mNodes.Count - 1);
                    }
                    await Task.Delay(Configuration.AUTO_RUN_SHUT_DOWN_AFTER);
                    await SaveLogs();
                    Application.Current.Shutdown();
                }, null);
            }
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
                var protocol = Configuration.CreateMACProtocol(mSeedGenerator.Next());
                var node = new Node(i, mNodeCount, protocol, new Random(mSeedGenerator.Next()));

                // We set this afterwards because we need a reference to node.
                protocol.SetSendAction(async (p, ct) => await mNetwork.StartTransmission(node, p, Packet.GetLength(p), ct));

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
            // Routing.GetShortestPath(mNodes, n => mNetwork.GetNodePosition(n), Configuration.PHYSICS_RANGE);
            if (int.TryParse(RouteStart.Text, out var nodeStart) && int.TryParse(RouteEnd.Text, out var nodeEnd))
            {
                if (nodeStart >= 0 && nodeEnd >= 0 && mNodes.Count > nodeStart && mNodes.Count > nodeEnd && nodeStart != nodeEnd)
                {
                    mNodes[nodeStart].StartRouteRequest(nodeEnd);
                }
            }
        }

        private async void Log_Click(object sender, RoutedEventArgs e)
        {
            await SaveLogs();
        }

        private async Task SaveLogs()
        {

            Debug.Write("Savelogs started");

            var lines = new List<string>
            {
                "FullyConnected,BackoffAlg,NodeCount,Traffic_Type,Traffic_parameter,Poisson_parameter,Type,SenderID,ReceiverID,SeqNum,AttemptNumber,TimeInitialSend,TimeSentOrReceived"
            };

            Debug.Write("LogVariable created");

            foreach (var node in mNodes)
            {
                lines.AddRange(node.GetLog());
            }

            Debug.Write("Getlog started");

            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            nfi.NumberDecimalDigits = 7;
            await File.WriteAllLinesAsync($"log-{DateTime.Now.ToString("s", CultureInfo.CreateSpecificCulture("de-DE")).Replace(":", "-")}-n-{mNodeCount}-BO-{Configuration.CA_BACKOFF}-FC-{Configuration.AUTO_RUN_FULLY_CONNECTED}-MCT-{Configuration.MESSAGE_CHANCE_TYPE}-PoisP-{Configuration.AUTO_RUN_POISSON_PARAMETER.ToString("N4", CultureInfo.CreateSpecificCulture("en-US"))}-Traf-{Configuration.AUTO_RUN_TRAFFIC.ToString("N4", CultureInfo.CreateSpecificCulture("en-US"))}.txt", lines);
            Debug.Write("WriteAllLines awaited.");
        }
    }
}
