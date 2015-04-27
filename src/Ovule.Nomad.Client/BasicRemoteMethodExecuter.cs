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
using System.Linq.Expressions;

namespace Ovule.Nomad.Client
{
  /// <summary>
  /// Pretty much the most basic form of RemoteExecutor you'll get which allows for a method 
  /// to be executed on an arbitrary server.
  /// </summary>
  public class BasicRemoteMethodExecuter : RemoteMethodExecuter
  {
    #region Properties/Fields

    private Uri _remoteUri;
    private IFaultRecoverer _faultRecoverer;

    #endregion Properties/Fields

    #region ctors

    public BasicRemoteMethodExecuter(Uri remoteUri)
      : this(remoteUri, null)
    {
    }

    public BasicRemoteMethodExecuter(Uri remoteUri, IFaultRecoverer faultRecoverer)
      : base()
    {
      this.ThrowIfArgumentIsNull(() => remoteUri);
      _remoteUri = remoteUri;
      _faultRecoverer = faultRecoverer;
    }

    #endregion ctors

    #region Convenience

    public object Execute(Expression<Action> operation)
    {
      try
      {
        return Execute(_remoteUri, operation);
      }
      catch
      {
        if (_faultRecoverer != null)
          return _faultRecoverer.TryRecover<object>(new Func<Uri, object>((uri) => Execute(uri, operation)));
        throw;
      }
    }

    public T Execute<T>(Expression<Action> operation)
    {
      try
      {
        return Execute<T>(_remoteUri, operation);
      }
      catch
      {
        if (_faultRecoverer != null)
          return _faultRecoverer.TryRecover<T>(new Func<Uri, T>((uri) => Execute<T>(uri, operation)));
        throw;
      }
    }

    public void ExecuteLocalAndRemote(Expression<Action> operation)
    {
      try
      {
        ExecuteLocalAndRemote(_remoteUri, operation);
      }
      catch
      {
        if (_faultRecoverer != null)
          //don't want to reattempt the local call, just the remote one
          _faultRecoverer.TryRecover(((uri) => Execute(uri, operation)));
        else
          throw;
      }
    }

    #endregion Convenience
  }
}