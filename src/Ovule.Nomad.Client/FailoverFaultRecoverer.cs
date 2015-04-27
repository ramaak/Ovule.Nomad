/*
Copyright (c) 2015 Tony Di Nucci (tonydinucci[at]gmail[dot]com)
 
This file is part of Nomad.

Nomad is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Nomad is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Nomad.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;

namespace Ovule.Nomad.Client
{
  public class FailoverFaultRecoverer : IFaultRecoverer
  {
    #region Properties/Fields

    public Uri[] FailoverUris { get; private set; }

    #endregion Properties/Fields

    #region ctors

    public FailoverFaultRecoverer(Uri[] failoverUris)
    {
      if (failoverUris == null || failoverUris.Length == 0)
        throw new ArgumentException("The 'failoverUris' argument has no value");
      FailoverUris = failoverUris;
    }

    #endregion ctors

    #region IFaultRecoverer

    public void TryRecover(Action<Uri> failedAction)
    {
      Func<Uri, object> exec = new Func<Uri, object>((uri) => { failedAction(uri); return null; });
      DoTryRecover(exec);
    }

    public T TryRecover<T>(Func<Uri, T> failedFunc)
    {
      Func<Uri, object> exec = new Func<Uri, object>((uri) => { return failedFunc(uri); });
      return (T)DoTryRecover(exec);
    }

    protected object DoTryRecover(Func<Uri, object> executeFunc)
    {
      List<Exception> retryExceptions = new List<Exception>();
      foreach (Uri failoverUri in FailoverUris)
      {
        try
        {
          return executeFunc(failoverUri);
        }
        catch (Exception ex)
        {
          retryExceptions.Add(ex);
          //just loop move onto the next iteration
        }
      }
      //if here then all retries failed
      throw new FaultRecoveryFailedException("All retry attempts failed, see RecoveryAttemptExceptions for more details", retryExceptions);

    }

    #endregion IFaultRecoverer
  }
}
