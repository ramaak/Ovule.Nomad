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
using System.Threading;

namespace Ovule.Nomad.Client
{
  public class RetryFaultRecoverer : IFaultRecoverer
  {
    #region Properties/Fields

    public int MaxRetries { get; private set; }
    public TimeSpan PauseBetweenRetries { get; private set; }

    #endregion Properties/Fields

    #region ctors

    public RetryFaultRecoverer(int maxRetries)
      : this(maxRetries, TimeSpan.Zero)
    {
    }

    public RetryFaultRecoverer(int maxRetries, TimeSpan pauseBetweenRetries)
    {
      this.ThrowIfArgumentNotPositive(() => maxRetries);

      MaxRetries = maxRetries;
      PauseBetweenRetries = pauseBetweenRetries;
    }

    #endregion ctors

    #region IFaultRecoverer

    public void TryRecover(Action failedAction)
    {
      Func<object> exec = new Func<object>(() => { failedAction(); return null; });
      DoTryRecover(exec);
    }

    public T TryRecover<T>(Func<T> failedFunc)
    {
      Func<object> exec = new Func<object>(() => { return failedFunc(); });
      return (T)DoTryRecover(exec);
    }

    protected object DoTryRecover(Func<object> executeFunc)
    {
      List<Exception> retryExceptions = new List<Exception>();
      for (int i = 0; i < MaxRetries; i++)
      {
        try
        {
          if (PauseBetweenRetries > TimeSpan.Zero)
            Thread.Sleep(PauseBetweenRetries);

          return executeFunc();
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
