using System.Diagnostics;
using System.Windows;

namespace AdHocMAC.GUI
{
    /// <summary>
    /// Presents low level event debug information to the user.
    /// </summary>
    class LogHandler
    {
        public void OnDebug(string Message)
        {
            Debug.WriteLine(Message);
        }

        public void OnDebug(int Node, string Message)
        {
            OnDebug($"Node {Node}: {Message}");
        }

        public void OnError(string Message)
        {
            MessageBox.Show(Message, "An Error Occurred", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void OnError(int Node, string Message)
        {
            OnError($"Node {Node}: {Message}");
        }
    }
}
