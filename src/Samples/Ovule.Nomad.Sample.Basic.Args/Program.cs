using System;

namespace Ovule.Nomad.Sample.Basic.Args
{
  class Program
  {
    static void Main(string[] args)
    {
      int a = 10;
      int b = 5;
      Console.WriteLine("{0} + {1} = {2}", a, b, Sum(a, b));
      Console.ReadLine();
    }

    [NomadMethod]
    private static int Sum(int a, int b)
    {
      Console.WriteLine("Calculating {0} + {1}", a, b);
      return a + b;
    }
  }
}
