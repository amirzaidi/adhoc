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

        public void BeginReceive(T Receiver, T Sender, (byte, byte, byte) RGB)
        {
            var (r, g, b) = RGB;
            mVisualizer.RunOnUIThread(() =>
            {
                //mVisualizer.ChangeLineColor(Sender, Receiver, 127, 255, 0);
                mVisualizer.ChangeBlobColor(Receiver, Sender, r, g, b);
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

        public void BeginSend(T Sender, (byte, byte, byte) RGB)
        {
            var (r, g, b) = RGB;
            mVisualizer.RunOnUIThread(() =>
            {
                mVisualizer.ChangeNodeColor(Sender, r, g, b);
                mVisualizer.ChangeNodeText(Sender, "Sending");
            });
        }

        public void EndSend(T Sender)
        {
            mVisualizer.RunOnUIThread(() =>
            {
                mVisualizer.ChangeNodeColor(Sender, 0, 0, 0);
                mVisualizer.ChangeNodeText(Sender, "Idle");
            });
        }
    }
}
