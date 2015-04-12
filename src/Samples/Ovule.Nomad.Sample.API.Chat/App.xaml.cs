using Ovule.Nomad.Server;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Threading;

namespace Ovule.Nomad.Sample.API.Chat
{
  public partial class App : Application
  {
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartP2PServer();
    }

    private void StartP2PServer()
    {
      Uri thisP2PEndpointUri = new Uri(ConfigurationManager.AppSettings["ThisP2PUri"]);
      using (NomadWcfServer server = new NomadWcfServer(thisP2PEndpointUri))
      {
        server.Start();
        User localUser = new User(thisP2PEndpointUri);
        new MainWindow(localUser).ShowDialog();
      }
    }

    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
      MessageBox.Show(string.Format("An error occurred and the application will shutdown\r\n{0}", e.Exception.Message));
      Application.Current.Shutdown();
    }
  }
}
