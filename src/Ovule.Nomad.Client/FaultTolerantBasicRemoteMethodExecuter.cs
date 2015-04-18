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
using System.Linq.Expressions;
using System.Net.NetworkInformation;

namespace Ovule.Nomad.Client
{
  /// <summary>
  /// This is an implementation of BasicRemoteMethodExecuter with added basic fault tolerance.
  /// 
  /// Currently two forms of fault correction are provided, retry and failover.
  /// </summary>
  public class FaultTolerantBasicRemoteMethodExecuter : BasicRemoteMethodExecuter
  {
    #region Properties/Fields

    private int _retryAttempts;
    private Uri[] _failoverUris;
    private int _initialPingTimeoutSeconds;

    #endregion Properties/Fields

    #region ctors

    /// <summary>
    /// Construct a FaultTolerantBasicRemoteMethodExecuter which will reattempt method execution against the same Uri a number 
    /// of times.
    /// If 'initialPingTimeoutSeconds' has a positive value then a ping (with that timeout) will be performed agains the host before
    /// attempting to open a channel to it.  This will reduce the overall time taken to determine if the host is down. If it's unlikely 
    /// that the host is down then don't use this as it will resut in additional network traffic which will typically be uncalled for.
    /// Even if connecting over WCF and the open timeout is low it can take a long time to determine if a host is down.
    /// </summary>
    /// <param name="remoteUri"></param>
    /// <param name="retryAttempts"></param>
    /// <param name="initialPingTimeoutSeconds"></param>
    public FaultTolerantBasicRemoteMethodExecuter(Uri remoteUri, int retryAttempts, int initialPingTimeoutSeconds = 0)
      : base(remoteUri)
    {
      //if <= 0 then we're operating as a BasicRemoteMethodExecuter
      if (retryAttempts > 0)
        _retryAttempts = retryAttempts;
      _initialPingTimeoutSeconds = initialPingTimeoutSeconds;
    }

    /// <summary>
    /// Construct a FaultTolerantBasicRemoteMethodExecuter which will reattempt execution of the method on a series of failover hosts, in the 
    /// order they are specified.
    /// of times.
    /// If 'initialPingTimeoutSeconds' has a positive value then a ping (with that timeout) will be performed agains the host before
    /// attempting to open a channel to it.  This will reduce the overall time taken to determine if the host is down. If it's unlikely 
    /// that the host is down then don't use this as it will resut in additional network traffic which will typically be uncalled for.
    /// Even if connecting over WCF and the open timeout is low it can take a long time to determine if a host is down.
    /// </summary>
    /// <param name="remoteUri"></param>
    /// <param name="failoverUris"></param>
    /// <param name="initialPingTimeoutSeconds"></param>
    public FaultTolerantBasicRemoteMethodExecuter(Uri remoteUri, Uri[] failoverUris, int initialPingTimeoutSeconds = 0)
      : base(remoteUri)
    {
      //if null or empty we're operating as a BasicRemoteMethodExecuter
      _failoverUris = failoverUris;
      _initialPingTimeoutSeconds = initialPingTimeoutSeconds;
    }

    #endregion ctors

    #region Execution

    /// <summary>
    /// Returns the appropriate Uri to attempt make an Excute(...) attempt on.
    /// If _retryAttempts > 0 then keep returning the main Uri _retryAttempt times after initial attempt.
    /// Otherwise, if _failoverUris.Length > 0 then return each fallback after the initial attempt.
    /// 
    /// If there is no suitable Uri to use, i.e. attempts are exhausted then null is returned.
    /// 
    /// Private method that assumes it's being called with valid args.
    /// </summary>
    /// <param name="preferredUri">The default Uri to use</param>
    /// <param name="executionAttempt">A 1 based number which specifies the attempt being made</param>
    /// <returns></returns>
    private Uri GetExecutionAttemptUri(Uri preferredUri, int executionAttempt)
    {
      if (executionAttempt == 1)
        return preferredUri;

      //if _retryAttempts is specified then just use the preferredUri - i.e. no failover
      if (_retryAttempts > 0)
      {
        if (executionAttempt > _retryAttempts)
          return null;
        return preferredUri;
      }

      //because we return at top if executionAttemp == 1 failoverAttempt will always be at least 1
      int failoverAttempt = executionAttempt - 1;
      if (_failoverUris != null && _failoverUris.Length >= failoverAttempt)
        return _failoverUris[failoverAttempt - 1];

      return null;
    }

    /// <summary>
    /// Override of BasicRemoteMethodExecuter.Execute(...).
    /// This will call into the base method first with using the Uri 'remoteUri' however if this 
    /// fails it will re-attempt the call, either using the same Uri if we are in "Retry" mode 
    /// or another Uri if we are in "Failover" mode.  
    /// If all reattempts are exhausted (and none succeeded) then a 
    /// FaultTolerantRemoteMethodNotExecutedException if thrown which contains a collection of the errors
    /// that happened with each attempt.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="remoteUri"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    private object AttemptExecution(Uri remoteUri, Func<Uri, object> executeFunc)
    {
      object result = null;
      Dictionary<Uri, IList<Exception>> executionExceptions = new Dictionary<Uri, IList<Exception>>();
      bool isSuccess = false;
      int currentAttempt = 0;
      do
      {
        isSuccess = false;
        Uri executionUri = GetExecutionAttemptUri(remoteUri, ++currentAttempt);
        if (executionUri == null)
        {
          throw new FaultTolerantRemoteMethodNotExecutedException("Failed to execute method directly or with fault tolerance measures. " +
            "ExecutionAttemptExceptions may contain more details", executionExceptions);
        }

        try
        {
          //attempting to opening a channel to a host that is down is expensive (can take ~20 seconds with WCF even if 
          //timeouts are much lower). If it's likely that any host will be down then it's cheaper to do a quick initial ping
          if (_initialPingTimeoutSeconds > 0)
          {
            PingReply pingReply = new Ping().Send(executionUri.Host, _initialPingTimeoutSeconds * 1000);
            if (pingReply.Status != IPStatus.Success)
              throw new PingException(string.Format("Failed to connect to host {0}", executionUri.Host));
          }

          result = executeFunc(executionUri);
          isSuccess = true;
        }
        catch (Exception ex)
        {
          if (ex is FaultTolerantRemoteMethodNotExecutedException)
            throw;

          //a general error occurred while executing, just handle here and loop round.
          //if the all re-attempts are exhaused then a FaultTolerantRemoteMethodNotExecutedException will
          //be thrown (and rethrown directly above) and we'll bomb out of this method.
          //Keep a record of these general errors so they can be reported with the FaultTolerantRemoteMethodNotExecutedException
          //if things come to that.
          if (!executionExceptions.ContainsKey(executionUri))
            executionExceptions.Add(executionUri, new List<Exception>());
          IList<Exception> uriExceptions = executionExceptions[executionUri];
          uriExceptions.Add(ex);
        }
      } while (!isSuccess);

      return result;
    }

    /// <summary>
    /// Fault tolerant override of base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="remoteUri"></param>
    /// <param name="operation"></param>
    /// <param name="operationArg"></param>
    public override void Execute<T>(Uri remoteUri, Action<T> operation, T operationArg)
    {
      Func<Uri, object> exec = new Func<Uri, object>((uri) => { base.Execute<T>(uri, operation, operationArg); return null; });
      AttemptExecution(remoteUri, exec);
    }

    /// <summary>
    /// Fault tolerant override of base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="remoteUri"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    public override T Execute<T>(Uri remoteUri, Expression<Action> operation)
    {
      Func<Uri, object> exec = new Func<Uri, object>((uri) => { return base.Execute<T>(uri, operation); });
      return (T)AttemptExecution(remoteUri, exec);
    }

    /// <summary>
    /// Fault tolerant override of base implementation
    /// </summary>
    /// <param name="remoteUri"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    public override object Execute(Uri remoteUri, Expression<Action> operation)
    {
      Func<Uri, object> exec = new Func<Uri, object>((uri) => { return base.Execute(uri, operation); });
      return AttemptExecution(remoteUri, exec);
    }

    #endregion Execution
  }
}
