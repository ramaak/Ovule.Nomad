using System;
using System.Numerics;

namespace Ovule.Nomad.Sample.Basic.NomadType
{
  class Program
  {
    static void Main(string[] args)
    {
      PiCalculator piCalc = new PiCalculator(100, 100);
      BigInteger pi = piCalc.GetPi();
      Console.WriteLine("{0} calculated Pi to {1} digits in {2} iterations as:{3}{4}", 
        piCalc.ProcessName, piCalc.GetDigitCount(), piCalc.GetIterations(), Environment.NewLine, pi);
      Console.ReadLine();
    }
  }
}
