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

namespace Ovule.Nomad.Client
{
  public abstract class RemoteMethodExecuter : IRemoteMethodExecuter
  {
    #region Cache

    private static Dictionary<string, byte[]> _rawAssemblies = new Dictionary<string, byte[]>();

    #endregion Cache

    #region IRemoteMethodExecuter

    /// <summary>
    /// Executes a standard nomadic method
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public abstract object Execute(Expression<Action> operation);

    /// <summary>
    /// Executes a standard nomadic method and returns the result as T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="operation"></param>
    /// <returns></returns>
    public abstract T Execute<T>(Expression<Action> operation);

    /// <summary>
    /// Executes a nomad method on the locally and also remotely.  
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public abstract void ExecuteLocalAndRemote(Expression<Action> operation);

    #endregion IRemoteMethodExecuter

    #region Supporting

    /// <summary>
    /// Calls through to the server
    /// </summary>
    /// <param name="methExp"></param>
    /// <param name="parameters"></param>
    /// <param name="rawAssembly"></param>
    /// <returns></returns>
    protected ExecuteServiceCallResult ExecuteServiceCall(Uri remoteEndpointUri, MethodCallExpression methExp, IList<ParameterVariable> parameters, byte[] rawAssembly)
    {
      ExecuteServiceCallResult result = null;
      if (methExp.Method.IsStatic)
        result = new NomadWcfClient().ExecuteStaticServiceCall(rawAssembly, remoteEndpointUri, NomadMethodType.Normal, false, methExp.Method.DeclaringType, methExp.Method.Name, parameters);
      else
      {
        Tuple<Type, object> typeVal = ExpressionUtils.Evaluate(methExp.Object);
        result = new NomadWcfClient().ExecuteServiceCall(rawAssembly, remoteEndpointUri, NomadMethodType.Normal, false, typeVal.Item2, methExp.Method.Name, parameters);
      }
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    protected IList<ParameterVariable> GetMethodParameters(Tuple<Type, object>[] paramTypeValues)
    {
      List<ParameterVariable> parameters = null;
      if (paramTypeValues != null && paramTypeValues.Length > 0)
      {
        parameters = new List<ParameterVariable>();
        for (int i = 0; i < paramTypeValues.Length; i++)
        {
          Tuple<Type, object> paramTypeValue = paramTypeValues[i];
          parameters.Add(new ParameterVariable(string.Format("p{0}", i), paramTypeValue.Item1.FullName, paramTypeValue.Item2));
        }
      }
      return parameters;
    }

    #endregion Supporting
  }
}
