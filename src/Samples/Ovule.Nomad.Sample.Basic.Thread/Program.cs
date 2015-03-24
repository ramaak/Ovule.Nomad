using System;
using System.Threading.Tasks;

namespace Ovule.Nomad.Sample.Basic.Thread
{
  class Program
  {
    static object _outputLocker = new object();

    static void Main(string[] args)
    {
      Task t = null;
      try
      {
        string output = string.Empty;
        t = Task.Factory.StartNew(() =>
        {
          for (int i = 1; i <= 10; i++)
          {
            lock (_outputLocker)
            {
              output += GetLine(i);
            }
            System.Threading.Thread.Sleep(25);
          }
        });

        for (int i = 11; i <= 20; i++)
        {
          lock (_outputLocker)
          {
            output += GetLine(i);
          }
          System.Threading.Thread.Sleep(25);
        }

        t.Wait();

        Console.WriteLine(output);
      }
      finally
      {
        t.Dispose();
      }
      Console.ReadLine();
    }

    [NomadMethod]
    static string GetLine(int lineId)
    {
      return string.Format("{0} - {1}{2}", lineId, System.Diagnostics.Process.GetCurrentProcess().ProcessName, Environment.NewLine);
    }
  }
}
