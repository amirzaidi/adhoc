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
using System.Linq;
using System.Text;
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
        private readonly Random mSeedGenerator = new Random(/* TOP_LEVEL_SEED */);

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
                mLogHandler.OnDebug($"Stopping {mNodes.Count} nodes");
                mCTS.Cancel();

                foreach (var nodeThread in mNodeThreads)
                {
                    await nodeThread;
                }
            }

            mNodeThreads.Clear();
            mNetwork.ClearNodes();

            mNodeCount = (int) NodeCount.Value;
            mLogHandler.OnDebug($"Creating {mNodeCount} new nodes");

            mNodes.Clear();

            mCTS = new CancellationTokenSource();
            for (int i = 0; i < mNodeCount; i++)
            {
                var protocol = Configuration.CreateMACProtocol(mSeedGenerator.Next());
                var node = new Node(i, mNodeCount, protocol, new Random(mSeedGenerator.Next()));

                // We set this afterwards because we need a reference to node.
                protocol.SetSendAction(async (p, ct) => await mNetwork.StartTransmission(node, p, Packet.GetLength(p), ct));

                // Add newly created node.
                mNodes.Add(node);
            }

            // Show everything in the UI.
            mNodeVisualizer.ResetNodes(new List<INode<Packet>>(mNodes));

            // Start all logic loops when they are added to the UI.
            // Use Task.Run to force each loop onto a separate execution context.
            mNodes.ForEach(node => mNodeThreads.Add(Task.Run(() => node.Loop(mCTS.Token))));

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
            Debug.WriteLine("SaveLogs Start");

            var enUS = CultureInfo.CreateSpecificCulture("en-US");
            var deDE = CultureInfo.CreateSpecificCulture("de-DE");

            var time = DateTime.Now.ToString("s", deDE).Replace(":", "-");
            var name = new StringBuilder()
                .Append(time)
                .Append("-SimTime-")
                .Append(Configuration.AUTO_RUN_SHUT_DOWN_AFTER)
                .Append("-n-")
                .Append(mNodeCount)
                .Append("-BO-")
                .Append(Configuration.CA_BACKOFF)
                .Append("-FC-")
                .Append(Configuration.AUTO_RUN_FULLY_CONNECTED)
                .Append("-MCT-")
                .Append(Configuration.MESSAGE_CHANCE_TYPE)
                .Append("-PoisP-")
                .Append(Configuration.AUTO_RUN_POISSON_PARAMETER.ToString("N4", enUS))
                .Append("-Traf-")
                .Append(Configuration.AUTO_RUN_TRAFFIC.ToString("N4", enUS))
                .Append("-PP-")
                .Append(Configuration.PPersistency.ToString("N4", enUS))
                .ToString();

            Debug.Write("Savelogs started");
            if (!Configuration.AUTO_RUN_PACKETS_ENABLED)
            {
                // RREQ and RREP at Source and Dest.
                // Length of the path found in RREP.
                var routinglines = new List<string>
                {
                    "NodeID, Timestamp, PPersistency, Type, SourceID, DestinationID, Route, SeqNum",
                };

                routinglines.AddRange(
                    mNodes.Select(x => (x.GetID(), x.GetRoutingLog())) // Logs with ID.
                        .SelectMany(x => x.Item2.Select(y => (x.Item1, y.Item1, y.Item2))) // Add ID to each log entry then merge logs.
                        .OrderBy(x => x.Item2) // Sort by time.
                        .Select(x => $"{x.Item1}, {x.Item2}, {Configuration.PPersistency}, {x.Item3}") // Format into strings.
                );

                await File.WriteAllLinesAsync($"routinglog-{name}-{DateTime.Now.Ticks}.txt", routinglines);
            }

            var lines = new List<string>
            {
                "FullyConnected,BackoffAlg,NodeCount,Traffic_Type,Traffic_parameter,Poisson_parameter,Type,SenderID,ReceiverID,SeqNum,AttemptNumber,TimeInitialSend,TimeSentOrReceived"
            };
            lines.AddRange(mNodes.Select(x => x.GetLog()).SelectMany(x => x));
            await File.WriteAllLinesAsync($"log-{name}-{DateTime.Now.Ticks}.txt", lines);

            Debug.WriteLine("SaveLogs End");
        }
    }
}
