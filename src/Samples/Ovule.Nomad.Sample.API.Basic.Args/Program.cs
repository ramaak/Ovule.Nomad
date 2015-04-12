using Ovule.Nomad.Client;
using System;

namespace Ovule.Nomad.Sample.API.Basic.Args
{
  class Program
  {
    static void Main(string[] args)
    {
      BasicRemoteMethodExecuter exec = new BasicRemoteMethodExecuter(new Uri("net.tcp://localhost:8557/NomadService"));

      int a = 10;
      int b = 5;
      Console.WriteLine("{0} + {1} = {2}", a, b, exec.Execute(() => Sum(a, b)));
      Console.ReadLine();
    }

    private static int Sum(int a, int b)
    {
      Console.WriteLine("Calculating {0} + {1}", a, b);
      return a + b;
    }
  }
}
