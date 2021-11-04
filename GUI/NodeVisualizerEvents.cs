using AdHocMAC.Simulation;

namespace AdHocMAC.GUI
{
    class NodeVisualizerEvents<T> : INetworkEventLogger<T>
    {
        private readonly NodeVisualizer<T> mVisualizer;

        public NodeVisualizerEvents(NodeVisualizer<T> Visualizer)
        {
            mVisualizer = Visualizer;
        }

        public void BeginReceive(T Receiver, T Sender)
        {
            mVisualizer.RunOnUIThread(() =>
            {
                //mVisualizer.ChangeLineColor(Sender, Receiver, 127, 255, 0);
                mVisualizer.ChangeBlobColor(Receiver, Sender, 127, 255, 0);
            });
        }

        public void EndReceive(T Receiver, T Sender)
        {
            mVisualizer.RunOnUIThread(() =>
            {
                //mVisualizer.ChangeLineColor(Sender, Receiver, 0, 0, 0);
                mVisualizer.ChangeBlobColor(Receiver, Sender, 0, 0, 0);
            });
        }

        public void BeginSend(T Sender)
        {
            mVisualizer.RunOnUIThread(
                () => mVisualizer.ChangeNodeColor(Sender, 0, 127, 255)
            );
        }

        public void EndSend(T Sender)
        {
            mVisualizer.RunOnUIThread(
                () => mVisualizer.ChangeNodeColor(Sender, 0, 0, 0)
            );
        }
    }
}
