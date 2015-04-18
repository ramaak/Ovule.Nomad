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
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ovule.Nomad.Client
{
  /// <summary>
  /// This is an implementation of BasicRemoteMethodExecuter with added basic fault tolerance.
  /// 
  /// Currently two forms of fault correction are provided, retry and fallback.
  /// 
  /// TODO: This class is not finished - checking in early to make merging pull request easier.
  /// </summary>
  public class FaultTolerantBasicRemoteMethodExecuter : BasicRemoteMethodExecuter
  {
    #region Properties/Fields

    private int _retryAttempts;
    private Uri[] _fallbackUris;

    #endregion Properties/Fields

    #region ctors

    public FaultTolerantBasicRemoteMethodExecuter(Uri remoteUri, int retryAttempts)
      : base(remoteUri)
    {
      //if <= 0 then we're operating as a BasicRemoteMethodExecuter
      if (retryAttempts > 0)
        _retryAttempts = retryAttempts;
    }

    public FaultTolerantBasicRemoteMethodExecuter(Uri remoteUri, Uri[] fallbackUris)
      : base(remoteUri)
    {
      //if null or empty we're operating as a BasicRemoteMethodExecuter
      _fallbackUris = fallbackUris;
    }

    #endregion ctors

    #region Execution

    /*
    public override T Execute<T>(Uri remoteUri, Expression<Action> operation)
    {
      Dictionary<Uri, IList<Exception>> executionExceptions = new Dictionary<Uri, IList<Exception>>();
      Uri attemptUri = remoteUri;
      bool isSuccess = false;
      bool isFinalAttempt = false;
      int currentAttempt = 0;
      do
      {
        currentAttempt++;

        //if _retryAttempts > 0 then there will be no fallback option
        if (currentAttempt > 1)
        {
          if (_retryAttempts > 0)
            isFinalAttempt = currentAttempt == _retryAttempts + 1;//adding 1 because it's retry attempts, not try attemps
          else if (_fallbackUris != null && _fallbackUris.Length > 0)
          {
            attemptUri = _fallbackUris[]
          }
        }


        isSuccess = false;
        try
        {
          T result = base.Execute<T>(remoteUri, operation);
          isSuccess = true;
        }
        catch (Exception ex)
        {
          if (!executionExceptions.ContainsKey(remoteUri))
            executionExceptions.Add(remoteUri, new List<Exception>());
          IList<Exception> uriExceptions = executionExceptions[remoteUri];
          uriExceptions.Add(ex);

          if (isFinalAttempt)
            throw new FaultTolerantRemoteMethodNotExecutedException("Failed to execute method directly or with fault tolerance measures. See ExecutionAttemptExceptions for more details",
              executionExceptions);
        }
      } while (!isSuccess);
    }
     */

    #endregion Execution
  }
}
