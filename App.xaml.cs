using System.Windows;

namespace AdHocMAC
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 5)
            {
                int arg = 0;

                Configuration.AUTO_RUN_SHUT_DOWN_AFTER = int.Parse(e.Args[arg++]);
                Configuration.AUTO_RUN_PACKETS_ENABLED = bool.Parse(e.Args[arg++]);
                Configuration.AUTO_RUN_NODE_COUNT = int.Parse(e.Args[arg++]);
                Configuration.AUTO_RUN_FULLY_CONNECTED = bool.Parse(e.Args[arg++]);
                Configuration.MAC = (Configuration.MACProtocol)int.Parse(e.Args[arg++]);
            }

            MainWindow wnd = new MainWindow();
            wnd.Show();
        }
    }
}
