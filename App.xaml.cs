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
            if (e.Args.Length == 4)
            {
                int arg = 0;

                Configuration.AUTO_RUN_SHUT_DOWN_AFTER = int.Parse(e.Args[arg++]);
                Configuration.NODE_COUNT = int.Parse(e.Args[arg++]);
                Configuration.PPersistency = double.Parse(e.Args[arg++]);
                Configuration.MAC = (Configuration.MACProtocol)int.Parse(e.Args[arg++]);
            }

            MainWindow wnd = new MainWindow();
            wnd.Show();
        }
    }
}
