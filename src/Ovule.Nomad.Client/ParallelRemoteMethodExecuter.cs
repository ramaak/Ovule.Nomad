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
using System.Threading.Tasks;

namespace Ovule.Nomad.Client
{
  public delegate RemoteJob GetRemoteJobPartFunc(int part, int of);

  public class ParallelRemoteMethodExecuter
  {
    #region Properties/Fields

    private Uri[] _remoteUris;
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

    #region Methods

    /// <summary>
    /// Takes a GetRemoteJobPartFunc and calls it once per remote Uri that's known so obtaining 
    /// a RemoteJob each time.  Each RemoteJob is sent to one of the remote servers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="getDistributedJobPart"></param>
    /// <returns></returns>
    public T[] DistributeOperation<T>(GetRemoteJobPartFunc getDistributedJobPart)
    {
      this.ThrowIfArgumentIsNull(() => getDistributedJobPart);
      T[] result = new T[_remoteUris.Length];
      Parallel.For(0, _remoteUris.Length, (i) =>
      {
        RemoteJob part = getDistributedJobPart(i + 1, _remoteUris.Length);
        result[i] = (T)_exec.Execute(_remoteUris[i], part.Job);
      });
      return result;
    }

    /// <summary>
    /// Takes an array and splits even portions of it across all known servers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="action"></param>
    /// <param name="data"></param>
    public void DistributeArray<T>(Action<T[]> action, T[] data)
    {
      DistributeArray<T>(action, data, null);
    }

    /// <summary>
    /// Takes an array and splits even portions of it across all known servers
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
      Parallel.For(0, _remoteUris.Length, (i) =>
      {
        int blockStart = i * blockSize;
        //array might not cleanly divisible by number of URI's
        if (i == _remoteUris.Length - 1)
          blockSize = data.Length - blockStart;

        T[] dataBlock = new T[blockSize];
        Array.Copy(data, blockStart, dataBlock, 0, blockSize);

        _exec.Execute(_remoteUris[i], action, dataBlock);
      });
    }

    #endregion Methods
  }
}
