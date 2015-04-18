using Ovule.Nomad.Client;
using System;
using System.Collections.Generic;
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
        FaultTolerantBasicRemoteMethodExecuter exec = new FaultTolerantBasicRemoteMethodExecuter(defaultUri, failoverUris);
        Console.WriteLine(exec.Execute(() => SayHello()));
      }
      catch (FaultTolerantRemoteMethodNotExecutedException ex)
      {
        //this type of exception will only be thrown if no host can execute method.  In order for this to happen 
        //run this program without the stock server.  This is just a demonstration of how to dig into the problems
        Console.WriteLine("Failed to execute method: {0}", ex.Message);
        if (ex.ExecutionAttemptExceptions != null && ex.ExecutionAttemptExceptions.Count > 0)
        {
          foreach (KeyValuePair<Uri, IList<Exception>> uriExceptions in ex.ExecutionAttemptExceptions)
          {
            Console.WriteLine("Problem with Uri {0} was {1}", uriExceptions.Key, uriExceptions.Value[0].Message);
          }
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
