﻿using AdHocMAC.GUI;
using AdHocMAC.Nodes;
using AdHocMAC.Nodes.MAC;
using AdHocMAC.Simulation;
using AdHocMAC.Utility;
using System.Collections.Generic;
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
        // Fields to handle showing what is happening to the user.
        private readonly LogHandler mLogHandler = new LogHandler();
        private readonly NodeVisualizer<Node> mNodeVisualizer;

        // The real-time network simulation.
        private readonly SimulatedNetwork<Packet> mSimulatedNetwork = new SimulatedNetwork<Packet>();

        // Prevent doing multiple Resets simultaneously.
        private readonly DuplicateRunDiscarder mReset;

        // We use these two to stop all running nodes at the same time.
        private CancellationTokenSource mCTS;
        private readonly List<Task> mNodeThreads = new List<Task>();

        private int mNodeCount = 10;
        private double mPPersistency = 0.01;

        public MainWindow()
        {
            InitializeComponent();
            mNodeVisualizer = new NodeVisualizer<Node>(
                this,
                Grid.Children,
                (node, x, y) => mSimulatedNetwork.SetNodeAt(node, Point3D.Create(x, y))
            );

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
            }

            mNodeCount = (int) NodeCount.Value;
            mLogHandler.OnDebug($"Creating {mNodeCount} new nodes");
            mNodeThreads.Clear();

            var nodes = new List<Node>();

            mCTS = new CancellationTokenSource();
            for (int i = 0; i < mNodeCount; i++)
            {
                // To-Do: Make the outgoing packet function use the SimulatedNetwork instead of only logging.
                var protocol = new CarrierSensingPPersistent
                (
                    // This should become a lambda that directly transmits on the network (rather than logging).
                    Packet => mLogHandler.OnDebug(i, $"Sending: {Packet.Data} to {Packet.To}"),
                    mPPersistency
                );

                // Add newly created node.
                nodes.Add(new Node(i, protocol));
            }

            // Show everything in the UI.
            mNodeVisualizer.ResetNodes(nodes);

            // Start all logic loops when they are added to the UI.
            foreach (var node in nodes)
            {
                // Use Task.Run to force each loop onto a separate execution context.
                mNodeThreads.Add(Task.Run(() => node.Loop(mCTS.Token)));
            }

            mLogHandler.OnDebug($"Started {mNodeThreads.Count} nodes");
        }
    }
}
