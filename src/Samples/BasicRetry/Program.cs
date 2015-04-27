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
      Uri remoteUri = new Uri("net.tcp://localhost:8557/NomadService");
      BasicRemoteMethodExecuter exec = new BasicRemoteMethodExecuter(remoteUri, new RetryFaultRecoverer(remoteUri, 3));
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
