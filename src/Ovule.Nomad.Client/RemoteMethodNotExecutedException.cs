using System;

namespace Ovule.Nomad.Client
{
  public class RemoteMethodNotExecutedException: NomadException
  {
    public RemoteMethodNotExecutedException(string message) : base(message) { }
    public RemoteMethodNotExecutedException(string message, Exception innerException) : base(message, innerException) { }
  }
}
