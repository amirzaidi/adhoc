using System.Diagnostics;

namespace AdHocMAC.GUI
{
    /// <summary>
    /// Presents low level event debug information to the user.
    /// </summary>
    class LogHandler
    {
        public void OnEvent(int Node, string Message)
        {
            // To-Do: Print this in the window.
            Debug.WriteLine($"Node {Node}: {Message}");
        }
    }
}
