﻿using Ovule.Nomad.Client;
using System;

namespace Ovule.Nomad.Sample.API.Basic.HelloWorld
{
  class Program
  {
    static void Main(string[] args)
    {
      BasicRemoteMethodExecuter exec = new BasicRemoteMethodExecuter(new Uri("net.tcp://localhost:8557/NomadService"));

      //as the method name suggests, SayHello() is executed both on the local machine and remotely
      exec.ExecuteLocalAndRemote(() => SayHello());
      Console.ReadLine();
    }

    static void SayHello()
    {
      Console.WriteLine("Hello from process '{0}'!", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
    }
  }
}
