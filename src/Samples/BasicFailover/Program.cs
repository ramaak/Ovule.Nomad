using Ovule.Nomad.Client;
using System;
using System.Diagnostics;

namespace BasicFailover
{
  class Program
  {
    static void Main(string[] args)
    {
      Uri defaultUri = new Uri("net.tcp://999:8557/NomadService");
      Uri[] failoverUris = new Uri[] { new Uri("net.tcp://888:8557/NomadService"), new Uri("net.tcp://localhost:8557/NomadService") };

      try
      {
        //this will attempt to execute the method with a bad defaultUri.  It'll failover and the first failover will also be a bad host.
        //the second failover will be a good host (assuming you've got the stock server running!)
        BasicRemoteMethodExecuter exec = new BasicRemoteMethodExecuter(defaultUri, new FailoverFaultRecoverer(failoverUris));
        Console.WriteLine(exec.Execute(() => SayHello()));
      }
      catch (FaultRecoveryFailedException ex)
      {
        //this type of exception will only be thrown if no host can execute method.  In order for this to happen 
        //run this program without the stock server.  This is just a demonstration of how to dig into the problems
        Console.WriteLine("Failed to execute method: {0}", ex.Message);
        if (ex.RecoveryAttemptExceptions != null && ex.RecoveryAttemptExceptions.Count > 0)
        {
          foreach (Exception recoveryAttemptException in ex.RecoveryAttemptExceptions)
            Console.WriteLine("Error during failover: {0}", recoveryAttemptException.Message);
        }
      }
      Console.ReadLine();
    }

    static string SayHello()
    {
      return string.Format("Hello from {0}", Process.GetCurrentProcess().ProcessName);
    }
  }
}
