using System;

namespace Ovule.Nomad.Sample.Basic.Chain
{
  class Program
  {
    static void Main(string[] args)
    {
      string seed = "Hello";
      Console.WriteLine(MethodOne(seed));
      Console.WriteLine(MethodTwo(seed));
      Console.WriteLine(MethodThree(seed));

      Console.ReadLine();
    }

    [NomadMethod]
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
