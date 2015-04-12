using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovule.Nomad.Discovery
{
  public class NomadDiscoveryException: NomadException
  {
    public NomadDiscoveryException(string message) : base(message) { }
    public NomadDiscoveryException(string message, params object[] formatArgs) : base(string.Format(message, formatArgs)) { }
    
    public NomadDiscoveryException(string message, Exception innerException) : base(message, innerException) { }
    public NomadDiscoveryException(Exception innerException, string message, params object[] formatArgs) : base(string.Format(message, formatArgs), innerException) { }

  }
}
