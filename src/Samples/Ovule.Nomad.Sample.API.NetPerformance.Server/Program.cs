using Ovule.Nomad.Sample.API.NetPerformance.Shared;
using System;
using System.ServiceModel;

namespace Ovule.Nomad.Sample.API.NetPerformance.Server
{
  public class MyMathsService : IMyMathsService
  {
    public int Add(int a, int b) { return a + b; }

    public int Subtract(int a, int b) { return a - b; }
  }

  class Program
  {
    static void Main(string[] args)
    {
      ServiceHost svcHost = new ServiceHost(typeof(MyMathsService), new Uri("net.tcp://localhost:9090/MathsService"));
      svcHost.Open();

      Console.WriteLine("Hit return to shutdown");
      Console.ReadLine();
    }
  }
}
