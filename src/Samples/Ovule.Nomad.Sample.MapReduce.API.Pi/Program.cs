using Ovule.Nomad.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ovule.Nomad.Sample.MapReduce.API.Pi
{
  public class Program
  {
    private static readonly object _lock = new object();
    private const int PiDigits = 3000;

    private static void Main(string[] args)
    {
      Uri[] remoteUris = new Uri[] { 
        new Uri("net.tcp://192.168.20.45:8557/NomadService"),
        new Uri("net.tcp://192.168.0.5:8557/NomadService"),
        new Uri("net.tcp://localhost:8557/NomadService"), 
      };

      Stopwatch sw = Stopwatch.StartNew();

      SortedDictionary<int, string> piParts = new SortedDictionary<int, string>();
      string result = "";
      //Parallel.For(0, 2, (i) =>
      //  {
      //    IDictionary<int, string> workerPiParts = MapReduce(PiDigits, i, 2);
      //    lock (_lock)
      //    {
      //      foreach (KeyValuePair<int, string> workerPart in workerPiParts)
      //      {
      //        piParts.Add(workerPart.Key, workerPart.Value);
      //      }
      //    }
      //  });
      ParallelRemoteMethodExecuter exec = new ParallelRemoteMethodExecuter(remoteUris);
      IDictionary<int, string>[] workerResults = exec.DistributeOperation<IDictionary<int, string>>(GetRemoteJobPart);
      foreach (IDictionary<int, string> workerResult in workerResults)
      {
        foreach (KeyValuePair<int, string> workerPart in workerResult)
        {
          piParts.Add(workerPart.Key, workerPart.Value);
        }
      }

      result = "3.";
      foreach (KeyValuePair<int, string> piPart in piParts)
      {
        result += piPart.Value;
      }
      Console.WriteLine(result);

      sw.Stop();
      Console.WriteLine(">>> {0}ms", sw.ElapsedMilliseconds);
      Console.ReadLine();
    }

    private static RemoteJob GetRemoteJobPart(int part, int of)
    {
      //this RemoteJob will be executed on one of the remote nodes
      return new RemoteJob(() => MapReduce(PiDigits, part, of));
    }

    private static IDictionary<int, string> MapReduce(int piDigits, int workerNo, int ofWorkers)
    {
      Thread.Sleep(100);
      return Reduce(Map(piDigits, workerNo, ofWorkers));
    }

    private static IList<int> Map(int piDigits, int workerNo, int ofWorkers)
    {
      List<int> assignedBlockStartingPositions = new List<int>();
      int nextStartingPosition = workerNo * 9;
      do
      {
        assignedBlockStartingPositions.Add(nextStartingPosition);
        nextStartingPosition += ofWorkers * 9;
      } while (nextStartingPosition < piDigits);
      return assignedBlockStartingPositions;
    }

    private static IDictionary<int, string> Reduce(IList<int> blockStartingPositions)
    {
      ConcurrentDictionary<int, string> piParts = new ConcurrentDictionary<int, string>();
      Parallel.ForEach(blockStartingPositions, (i) =>
        {
          string piPart = CalcPi(i, i + 9);
          piParts[i] = piPart;
        });
      return piParts;
    }

    private static string CalcPi(int fromDigit, int toDigit)
    {
      StringBuilder piPart = new StringBuilder(toDigit - fromDigit);
      for (int i = fromDigit; i < toDigit; i += 9)
      {
        int nineDigits = NineDigitsOfPi.StartingAt(i + 1);
        int digitCount = Math.Min(toDigit - i, 9);
        string ds = string.Format("{0:D9}", nineDigits);
        piPart.Append(ds.Substring(0, digitCount));
      }
      Console.WriteLine(piPart.ToString());
      return piPart.ToString();
    }
  }
}
