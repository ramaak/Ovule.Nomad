using Ovule.Nomad.Client;
using System;
using System.Linq;

namespace Ovule.Nomad.Sample.API.Basic.MemberVariables
{
  class Program
  {
    private static string ProcessName { get; set; }
    private static string _alphabet;

    static void Main(string[] args)
    {
      BasicRemoteMethodExecuter exec = new BasicRemoteMethodExecuter(new Uri("net.tcp://localhost:8557/NomadService"));

      _alphabet = "abcdefg";

      //when this method runs remotely it will see _alphabet with the value "abcdefg"
      exec.Execute(() => ProcessMemberVariables());
      Console.WriteLine("Process name is '{0}'.{1}Reversed alphabet is '{2}'", ProcessName, Environment.NewLine, _alphabet);
      Console.ReadLine();
    }

    static void ProcessMemberVariables()
    {
      //the caller will see ProcessName read as the remote host process name
      ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

      //the caller will see this change to _alphabet
      _alphabet = new string(_alphabet.Reverse().ToArray());
    }
  }
}
