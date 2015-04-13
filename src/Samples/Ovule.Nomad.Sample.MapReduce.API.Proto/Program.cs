using System;

namespace Ovule.Nomad.Sample.MapReduce.API.Proto
{
  class Program
  {
    static void Main(string[] args)
    {
      string corpusPath = @"C:\Users\adinucci\Documents\GitHub\Ovule.Nomad\src\Samples\Ovule.Nomad.Sample.MapReduce.API.Proto\bin\Debug\TestCorpus.txt";
      char countChar = 'a';

      int result = new CharCounter().Run(corpusPath, countChar);
      Console.WriteLine("Counted '{0}' occurences of '{1}'", result, countChar);
      Console.ReadLine();
    }
  }
}
