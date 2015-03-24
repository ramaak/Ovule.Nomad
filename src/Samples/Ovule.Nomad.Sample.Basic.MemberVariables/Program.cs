using System;
using System.Linq;

namespace Ovule.Nomad.Sample.Basic.MemberVariables
{
  class Program
  {
    private static string ProcessName { get; set; }
    private static string _alphabet;

    static void Main(string[] args)
    {
      _alphabet = "abcdefg";
      ProcessMemberVariables();
      Console.WriteLine("Process name is '{0}'.{1}Reversed alphabet is '{2}'", ProcessName, Environment.NewLine, _alphabet);
      Console.ReadLine();
    }

    [NomadMethod]
    static void ProcessMemberVariables()
    {
      ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
      _alphabet = new string(_alphabet.Reverse().ToArray());
    }
  }
}
