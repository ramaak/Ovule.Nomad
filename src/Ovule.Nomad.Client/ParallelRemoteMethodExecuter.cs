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
using System.Threading;

namespace Ovule.Nomad.Client
{
  public class ParallelRemoteMethodExecuter : IRemoteMethodExecuter, IDisposable
  {
    #region Properties/Fields

    private Uri[] _remoteUris;
    private CountdownEvent _countdown;
    private RemoteMethodExecuter _exec = new RemoteMethodExecuter();

    #endregion Properties/Fields

    #region ctors

    public ParallelRemoteMethodExecuter(Uri[] remoteUris)
    {
      if (remoteUris == null || remoteUris.Length < 2)
        throw new ArgumentException("The 'remoteUris' argument must contain at least 2 Uri's");
      _remoteUris = remoteUris;
    }

    #endregion ctors

    #region IRemoteMethodExecuter

    private void ExecuteAsync<T>(Uri remoteUri, Action<T> operation, T operationArg)
    {
      _countdown.AddCount();
      ThreadPool.QueueUserWorkItem((state) =>
        {
          try
          {
            _exec.Execute(remoteUri, operation, operationArg);
          }
          finally
          {
            _countdown.Signal();
          }
        });
    }

    object IRemoteMethodExecuter.Execute(Uri remoteUri, Expression<Action> operation)
    {
      _countdown.AddCount();
      try
      {
        object result = _exec.Execute(remoteUri, operation);
        return result;
      }
      finally
      {
        _countdown.Signal();
      }
    }

    T IRemoteMethodExecuter.Execute<T>(Uri remoteUri, Expression<Action> operation)
    {
      _countdown.AddCount();
      try
      {
        T result = _exec.Execute<T>(remoteUri, operation);
        return result;
      }
      finally
      {
        _countdown.Signal();
      }
    }

    void IRemoteMethodExecuter.ExecuteLocalAndRemote(Uri remoteUri, Expression<Action> operation)
    {
      _countdown.AddCount();
      try
      {
        _exec.ExecuteLocalAndRemote(remoteUri, operation);
      }
      finally
      {
        _countdown.Signal();
      }
    }

    #endregion IRemoteMethodExecuter

    #region Methods

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="action"></param>
    /// <param name="data"></param>
    public void DistributeArray<T>(Action<T[]> action, T[] data)
    {
      DistributeArray<T>(action, data, null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="action"></param>
    /// <param name="data"></param>
    /// <param name="timeout"></param>
    public void DistributeArray<T>(Action<T[]> action, T[] data, TimeSpan? timeout)
    {
      this.ThrowIfArgumentIsNull(() => action);
      if (data == null || data.Length == 0)
        throw new ArgumentException("The 'data' argument contains no data");

      int blockSize = data.Length / _remoteUris.Length;
      using (_countdown = new CountdownEvent(1))
      {
        for (int i = 0; i < _remoteUris.Length; i++)
        {
          int blockStart = i * blockSize;
          //array might not cleanly divisible by number of URI's
          if (i == _remoteUris.Length - 1)
            blockSize = data.Length - blockStart;

          T[] dataBlock = new T[blockSize];
          Array.Copy(data, blockStart, dataBlock, 0, blockSize);

          ExecuteAsync<T[]>(_remoteUris[i], action, dataBlock);
        }
        _countdown.Signal();

        if (timeout.GetValueOrDefault(TimeSpan.Zero).TotalMilliseconds > 0)
          Wait(timeout.Value);
        else
          Wait();
      }
    }

    public void Wait()
    {
      _countdown.Wait();
    }

    public void Wait(TimeSpan timeout)
    {
      _countdown.Wait(timeout);
    }

    #endregion Methods

    #region IDisposable

    public void Dispose()
    {
      _countdown.Dispose();
    }

    #endregion IDisposable
  }
}
