using System;
using System.Collections.Generic;

namespace Ovule.Nomad.Client
{
  public class FaultTolerantRemoteMethodNotExecutedException : RemoteMethodNotExecutedException
  {
    public IDictionary<Uri, IList<Exception>> ExecutionAttemptExceptions { get; private set; }

    public FaultTolerantRemoteMethodNotExecutedException(string message, IDictionary<Uri, IList<Exception>> executionAttemptExceptions)
      : base(message)
    {
      ExecutionAttemptExceptions = executionAttemptExceptions;
    }
  }
}
