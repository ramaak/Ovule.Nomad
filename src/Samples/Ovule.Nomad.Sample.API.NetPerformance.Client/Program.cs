using Ovule.Nomad.Client;
using Ovule.Nomad.Sample.API.NetPerformance.Shared;
using System;
using System.ServiceModel;

namespace Ovule.Nomad.Sample.API.NetPerformance.Client
{
  /// <summary>
  /// Please refer to Wiki article for details: https://github.com/tony-dinucci/Ovule.Nomad/wiki/Example-7:-Improve-Performance-of-Existing-Application
  /// </summary>
  class Program
  {
    //make sure to have the stock server running as well as the Ovule.Nomad.Sample.API.NetPerformance.Server server
    static void Main(string[] args)
    {
      //this will be used to fire the method request to the stock server
      BasicRemoteMethodExecuter exec = new BasicRemoteMethodExecuter(new Uri("net.tcp://localhost:8557/NomadService"));

      //use the BasicRemoteMethodExecuter so that this method is executed on the server
      //meaning only one trip over the network
      int result = exec.Execute<int>(() => DoSomeMaths());
      Console.WriteLine("Result: {0}", result);
      Console.ReadLine();
    }

    static int DoSomeMaths()
    {
      //this will execute on the stock server and it'll then call through to the MathsService - which will be on the same machine
      //and therefore no further network activity is needed
      ChannelFactory<IMyMathsService> chFact = new ChannelFactory<IMyMathsService>(new NetTcpBinding(), "net.tcp://localhost:9090/MathsService");
      try
      {
        IMyMathsService mathsService = chFact.CreateChannel();

        int result = mathsService.Add(1, 2);
        result = mathsService.Add(result, 3);
        result = mathsService.Subtract(result, 4);
        result = mathsService.Add(result, 5);

        return result;
      }
      finally
      {
        chFact.Close();
      }
    }
  }
}
