using Ovule.Nomad.Client;
using System;

namespace Ovule.Nomad.Sample.API.Basic.Chain
{
  class Program
  {
    static void Main(string[] args)
    {
      BasicRemoteMethodExecuter exec = new BasicRemoteMethodExecuter(new Uri("net.tcp://localhost:8557/NomadService"));

      string seed = "Hello";
      //MethodOne(...) is executed remotely, so are the methods it call, i.e. MethodTwo(...) and MethodThree(...)
      Console.WriteLine(exec.Execute(() => MethodOne(seed)));

      //The following calls are executed on the client
      Console.WriteLine(MethodTwo(seed));
      Console.WriteLine(MethodThree(seed));

      Console.ReadLine();
    }

    static string MethodOne(string value)
    {
      return string.Format("{0}1 {1}\r\n\t{2}", value, System.Diagnostics.Process.GetCurrentProcess().ProcessName, MethodTwo(value));
    }

    static string MethodTwo(string value)
    {
      return string.Format("{0}2 {1}\r\n\t\t{2}", value, System.Diagnostics.Process.GetCurrentProcess().ProcessName, MethodThree(value));
    }

    static string MethodThree(string value)
    {
      return string.Format("{0}3 {1}", value, System.Diagnostics.Process.GetCurrentProcess().ProcessName);
    }
  }
}
