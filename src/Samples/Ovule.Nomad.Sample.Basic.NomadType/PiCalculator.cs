using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Ovule.Nomad.Sample.Basic.NomadType
{
  /// <summary>
  /// Based on http://stackoverflow.com/questions/11677369/how-to-calculate-pi-to-n-number-of-places-in-c-sharp-using-loops
  /// </summary>
  [NomadType]
  public class PiCalculator
  {
    private int _digits;
    private int _iterations;
    public string ProcessName { get; private set; }

    /// <summary>
    /// There must be a public default constructor in order for the Nomad Server to be able to 
    /// work with a type
    /// </summary>
    public PiCalculator() { }

    public PiCalculator(int digits, int iterations)
    {
      _digits = digits;
      _iterations = iterations;
    }

    public BigInteger GetPi()
    {
      //If we're currently on the server then this method call too will happen on the server
      BigInteger pi = 16 * ArcTan1OverX(5, _digits).ElementAt(_iterations) - 4 * ArcTan1OverX(239, _digits).ElementAt(_iterations);

      Console.WriteLine("Calculated Pi");
      ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

      return pi;
    }

    public int GetDigitCount()
    {
      Console.WriteLine("GetDigitCount()");
      return _digits;
    }

    public int GetIterations()
    {
      Console.WriteLine("GetIterations()");
      return _iterations;
    }

    private IEnumerable<BigInteger> ArcTan1OverX(int x, int digits)
    {
      var mag = BigInteger.Pow(10, digits);
      var sum = BigInteger.Zero;
      bool sign = true;
      for (int i = 1; true; i += 2)
      {
        var cur = mag / (BigInteger.Pow(x, i) * i);
        if (sign)
        {
          sum += cur;
        }
        else
        {
          sum -= cur;
        }
        yield return sum;
        sign = !sign;
      }
    }
  }
}
