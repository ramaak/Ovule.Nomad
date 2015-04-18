using Ovule.Nomad;
using Ovule.Nomad.Client;
using System;
using System.Diagnostics;

namespace BasicRetry
{
  class Program
  {
    //decoarate fields/properties you don't want to travel over network with [NomadIgnore]
    [NomadIgnore]
    static int _attempts = 0;

    static void Main(string[] args)
    {
      //the FaultTolerantBasicRemoteMethodExecuter perform up to 4 attempts (orig call + 3 retries) to execute any methods.
      //There will be a pause of TimeSpan.Zero (in this example) between each retry.
      FaultTolerantBasicRemoteMethodExecuter exec = new FaultTolerantBasicRemoteMethodExecuter(new Uri("net.tcp://localhost:8557/NomadService"), 3, TimeSpan.Zero);
      Console.WriteLine(exec.Execute(() => ThreeIsMagic()));
      Console.ReadLine();
    }

    /// <summary>
    /// A very contrived example but this method will fail until it's called 3 times, demonstrating the retry feature
    /// of the FaultTolerantBasicRemoteMethodExecuter
    /// </summary>
    /// <returns></returns>
    static string ThreeIsMagic()
    {
      if (++_attempts != 3)
      {
        Console.WriteLine("Failing attempt {0}", _attempts);
        throw new InvalidOperationException("Something bad's just happened!");
      }
      _attempts = 0;
      return string.Format("Hello from {0}", Process.GetCurrentProcess().ProcessName);
    }
  }
}
