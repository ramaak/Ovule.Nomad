using Ovule.Nomad.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Ovule.Nomad.Sample.MapReduce.API.Pi
{
  public class Program
  {
    private const int PiDigits = 1000;

    /// <summary>
    /// This sample calulated pi to 'PiDigits', distributing the work evenly over a number of processes.
    /// </summary>
    /// <param name="args"></param>
    private static void Main(string[] args)
    {
      Uri[] remoteUris = new Uri[] { 
        new Uri("net.tcp://192.168.0.12:8557/NomadService"),
        new Uri("net.tcp://192.168.0.5:8557/NomadService"),
        new Uri("net.tcp://localhost:8557/NomadService"), 
      };

      Stopwatch sw = Stopwatch.StartNew();

      SortedDictionary<int, string> piParts = new SortedDictionary<int, string>();
      string result = "";
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
      return Reduce(Map(piDigits, workerNo, ofWorkers));
    }

    /// <summary>
    /// This method splits the job of calculating pi to a length of 'piDigits' 
    /// between 'ofWorkers' processes in an interleaved fashion.  It gets increasingly 
    /// hard to calculate parts of pi the further through the number you are and for this 
    /// reason it wouldn't be great to just dish out the first x% to process 1, the second 
    /// x% to process 2, etc as the later processes will have a much harder job than the 
    /// earlier ones, meaning we'd not be evenly distributing the load - leading to a longer 
    /// overall processing time.  
    /// 
    /// The result of this method will actually cause Pi to be generated to at least a length 
    /// of 'piDigits'.  
    /// </summary>
    /// <param name="piDigits"></param>
    /// <param name="workerNo"></param>
    /// <param name="ofWorkers"></param>
    /// <returns></returns>
    private static IList<int> Map(int piDigits, int workerNo, int ofWorkers)
    {
      List<int> assignedBlockStartingPositions = new List<int>();
      int nextStartingPosition = (workerNo - 1) * 9;
      do
      {
        assignedBlockStartingPositions.Add(nextStartingPosition);
        nextStartingPosition += ofWorkers * 9;
      } while (nextStartingPosition < piDigits);
      return assignedBlockStartingPositions;
    }

    /// <summary>
    /// This method accepts a block of work from the Map operation, i.e. 
    /// the work required of a single process.  It calculates the parts of 
    /// pi required and and returns them
    /// </summary>
    /// <param name="blockStartingPositions"></param>
    /// <returns></returns>
    private static IDictionary<int, string> Reduce(IList<int> blockStartingPositions)
    {
      ConcurrentDictionary<int, string> piParts = new ConcurrentDictionary<int, string>();
      Parallel.ForEach(blockStartingPositions, (i) =>
        {
          string piPart = GetPiPart(i, i + 9);
          piParts[i] = piPart;
        });
      return piParts;
    }

    /// <summary>
    /// Calculates and returns the 9 digits of pi between 'fromDigit' and 'toDigit'
    /// </summary>
    /// <param name="fromDigit"></param>
    /// <param name="toDigit"></param>
    /// <returns></returns>
    private static string GetPiPart(int fromDigit, int toDigit)
    {
      StringBuilder piPart = new StringBuilder(toDigit - fromDigit);
      for (int i = fromDigit; i < toDigit; i += 9)
      {
        int nineDigits = NineDigitsOfPi.StartingAt(i + 1);
        int digitCount = Math.Min(toDigit - i, 9);
        string digitString = string.Format("{0:D9}", nineDigits);
        piPart.Append(digitString.Substring(0, digitCount));
      }
      Console.WriteLine("{0} to {1}:\t\t{2}", fromDigit, toDigit, piPart.ToString());
      return piPart.ToString();
    }
  }
}
