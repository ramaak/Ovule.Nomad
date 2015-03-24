using System;

namespace Ovule.Nomad.Sample.Basic.HelloWorld
{
  class Program
  {
    static void Main(string[] args)
    {
      SayHello();
      Console.ReadLine();
    }

    [NomadMethod]
    static void SayHello()
    {
      Console.WriteLine("Hello from process '{0}'!", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
    }
  }
}
