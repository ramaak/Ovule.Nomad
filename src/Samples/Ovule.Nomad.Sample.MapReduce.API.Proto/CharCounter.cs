using Ovule.Nomad.Client;
using System;
using System.IO;

namespace Ovule.Nomad.Sample.MapReduce.API.Proto
{
  public class CharCounter
  {
    private Uri[] _remoteUris;
    private string _corpusPath;
    private char _countChar;
    private int _corpusLength;

    public CharCounter()
    {
      _remoteUris = new Uri[] { 
        new Uri("net.tcp://localhost:8557/NomadService"), new Uri("net.tcp://localhost:8558/NomadService"),
        new Uri("net.tcp://localhost:8559/NomadService"), new Uri("net.tcp://localhost:8560/NomadService")
      };
    }

    public int Run(string corpusPath, char countChar)
    {
      _corpusPath = corpusPath;
      _countChar = countChar;
      _corpusLength = (int)new FileInfo(_corpusPath).Length;
      ParallelRemoteMethodExecuter exec = new ParallelRemoteMethodExecuter(_remoteUris);

      int result = 0;

      //GetRemoteJobPart will be called once per remote node with values like 1/4, 2/4, etc.
      //DistributeOperation sends each RemoteJob to a seperate node and captures all results
      int[] results = exec.DistributeOperation<int>(GetRemoteJobPart);

      //a further simple reduce to sum the char counts
      foreach (int res in results)
        result += res;
      return result;
    }

    private RemoteJob GetRemoteJobPart(int part, int of)
    {
      int blockSize = _corpusLength / of;
      int blockStart = (part - 1) * blockSize;
      if (part == of)
        blockSize = _corpusLength - blockSize;

      //this RemoteJob will be executed on one of the remote nodes
      return new RemoteJob(() => MapReduce(_countChar, _corpusPath, blockStart, blockSize));
    }

    private int MapReduce(char countChar, string filePath, int startPos, int length)
    {
      int result = Reduce(countChar, Map(filePath, startPos, length));

      Console.WriteLine("Counted '{0}' occurences of '{1}'", result, countChar);
      return result;
    }

    private char[] Map(string filePath, int startPos, int length)
    {
      using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        using (StreamReader rdr = new StreamReader(fs))
        {
          char[] buffer = new char[length];
          fs.Position = startPos;
          int readChars = rdr.ReadBlock(buffer, 0, length);

          Console.WriteLine("Read '{0}' characters", readChars);
          return buffer;
        }
      }
    }

    private int Reduce(char searchChar, char[] chars)
    {
      int charCount = 0;
      foreach (char c in chars)
      {
        if (c == searchChar)
          charCount++;
      }
      return charCount;
    }
  }
}
