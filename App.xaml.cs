using System.Globalization;
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
            if (e.Args.Length == 8)
            {
                int arg = 0;

                NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
                nfi.NumberDecimalDigits = 4;

                Configuration.AUTO_RUN_SHUT_DOWN_AFTER = int.Parse(e.Args[arg++]);
                Configuration.AUTO_RUN_PACKETS_ENABLED = bool.Parse(e.Args[arg++]);
                Configuration.AUTO_RUN_NODE_COUNT = int.Parse(e.Args[arg++]);
                Configuration.AUTO_RUN_FULLY_CONNECTED = bool.Parse(e.Args[arg++]);
                Configuration.MESSAGE_CHANCE_TYPE = (Configuration.MessageChance) char.Parse(e.Args[arg++]);

                // Configuration.AUTO_RUN_TRAFFIC = double.Parse(e.Args[arg++],, CultureInfo.CreateSpecificCulture("en-US")); // This should be a number in [0,1], used both for Poisson and uniform traffic
                Configuration.AUTO_RUN_TRAFFIC = float.Parse(e.Args[arg++], NumberStyles.Float, nfi); // This should be a number in [0,1], used both for Poisson and uniform traffic

                Configuration.CA_BACKOFF = (Configuration.CABackoff) char.Parse(e.Args[arg++]);
                // Configuration.AUTO_RUN_POISSON_PARAMETER = double.Parse(e.Args[arg++],,CultureInfo.CreateSpecificCulture("en-US"));

                Configuration.AUTO_RUN_POISSON_PARAMETER = float.Parse(e.Args[arg++], NumberStyles.Float, nfi);

            }

            MainWindow wnd = new MainWindow();
            wnd.Show();
        }
    }
}
