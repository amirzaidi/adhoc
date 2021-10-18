using AdHocMAC.GUI;
using AdHocMAC.Nodes;
using AdHocMAC.Nodes.MAC;
using AdHocMAC.Simulation;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AdHocMAC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Fields to handle showing what is happening to the user.
        private readonly LogHandler mLogHandler = new LogHandler();
        private readonly NodeVisualizer mNodeVisualizer = new NodeVisualizer();

        // The real-time network simulation.
        private readonly SimulatedNetwork<Packet> mSimulatedNetwork = new SimulatedNetwork<Packet>();

        // We use these two to stop all running nodes at the same time.
        private CancellationTokenSource mCTS;
        private readonly List<Task> mNodeThreads = new List<Task>();

        private int mNodeCount = 10;
        private double mPPersistency = 0.01;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            await Reset();
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

            mLogHandler.OnDebug($"Creating {mNodeCount} new nodes");
            mNodeThreads.Clear();

            mCTS = new CancellationTokenSource();
            for (int i = 0; i < mNodeCount; i++)
            {
                // To-Do: Make the outgoing packet function use the SimulatedNetwork.
                var protocol = new CarrierSensingPPersistent
                (
                    // This should become a lambda that directly transmits on the network (rather than logging).
                    Packet => mLogHandler.OnDebug(i, $"Sending: {Packet.Data} to {Packet.To}"),
                    mPPersistency
                );

                // To-Do: Add this node to network simulation before starting the loop.
                var node = new Node(i, protocol);

                // Start logic loop.
                mNodeThreads.Add(Task.Run(() => node.Loop(mCTS.Token)));
            }

            mLogHandler.OnDebug($"Started {mNodeThreads.Count} nodes");
        }
    }
}
