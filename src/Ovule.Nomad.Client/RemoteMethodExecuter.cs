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
using Ovule.Nomad.Discovery;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Ovule.Nomad.Client
{
  public class RemoteMethodExecuter : IRemoteMethodExecuter
  {
    #region Cache

    //private static Dictionary<string, byte[]> _rawAssemblies = new Dictionary<string, byte[]>();

    #endregion Cache

    #region IRemoteMethodExecuter

    /// <summary>
    /// Executes a standard nomadic method
    /// 
    /// TODO: Caching!!!
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public virtual object Execute(Uri remoteUri, Expression<Action> operation)
    {
      this.ThrowIfArgumentIsNull(() => operation);

      MethodCallExpression methExp = operation.Body as MethodCallExpression;
      if (methExp == null)
        throw new NotSupportedException("The operation must be a direct method call");

      KeyValuePair<MethodInfo, Tuple<Type, object>[]> methodCall = ExpressionUtils.ResolveMethod(operation);
      IList<ParameterVariable> parameters = GetMethodParameters(methodCall.Value);

      //first try executing without passing through the raw assembly.  The server will cache any assemblies it's given 
      //so if this isn't the first call for method then this should succeed
      ExecuteServiceCallResult result = ExecuteServiceCall(remoteUri, methExp, parameters, null);

      //if this is the first call for the method that the servers seen it'll respond saying the assemblies missing so
      //we need to pass it along now
      if (result.IsAssemblyMissing)
      {
        byte[] rawAssembly = new AssemblyGenerator().GenerateAssemblyForMethod(methodCall.Key);
        if (rawAssembly == null || rawAssembly.Length == 0)
          throw new NomadClientException("Failed to generate raw assembly to send to server");

        result = ExecuteServiceCall(remoteUri, methExp, parameters, rawAssembly);
      }

      //is there a failure we can't recover from here?
      //this could be for a number of reasons, e.g. offline, configuration, ... 
      if (!result.IsExecuted)
        throw new RemoteMethodNotExecutedException("The remote method was not executed");

      return result.Result;
    }


    /// <summary>
    /// TODO: Tidy up!!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="remoteUri"></param>
    /// <param name="operation"></param>
    /// <param name="operationArg"></param>
    public virtual void Execute<T>(Uri remoteUri, Action<T> operation, T operationArg)
    {
      this.ThrowIfArgumentIsNull(() => operation);

      //first try executing without passing through the raw assembly.  The server will cache any assemblies it's given 
      //so if this isn't the first call for method then this should succeed
      ExecuteServiceCallResult result = ExecuteServiceCall(operation.Method.IsStatic, remoteUri, operation.Target, operation.Method.DeclaringType, operation.Method.Name,
        new List<ParameterVariable>() { new ParameterVariable("p1", typeof(T).FullName, operationArg) }, null);

      //if this is the first call for the method that the servers seen it'll respond saying the assemblies missing so
      //we need to pass it along now
      if (result.IsAssemblyMissing)
      {
        byte[] rawAssembly = new AssemblyGenerator().GenerateAssemblyForMethod(operation.Method);
        if (rawAssembly == null || rawAssembly.Length == 0)
          throw new NomadClientException("Failed to generate raw assembly to send to server");

        result = ExecuteServiceCall(operation.Method.IsStatic, remoteUri, operation.Target, operation.Method.DeclaringType, operation.Method.Name,
        new List<ParameterVariable>() { new ParameterVariable("p1", typeof(T).FullName, operationArg) }, rawAssembly);
      }

      //is there a failure we can't recover from here?
      //this could be for a number of reasons, e.g. offline, configuration, ... 
      if (!result.IsExecuted)
        throw new RemoteMethodNotExecutedException("The remote method was not executed");
    }

    /// <summary>
    /// Executes a standard nomadic method and returns the result as T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="operation"></param>
    /// <returns></returns>
    public virtual T Execute<T>(Uri remoteUri, Expression<Action> operation)
    {
      return (T)Execute(remoteUri, operation);
    }

    /// <summary>
    /// Executes a nomad method on the locally and also remotely.  
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public virtual void ExecuteLocalAndRemote(Uri remoteUri, Expression<Action> operation)
    {
      AutoResetEvent done = new AutoResetEvent(false);
      ThreadPool.QueueUserWorkItem((state) => { Execute(remoteUri, operation); done.Set(); });
      operation.Compile()();

      done.WaitOne();
    }

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
      object actOn = null;
      if (!methExp.Method.IsStatic)
      {
        Tuple<Type, object> typeVal = ExpressionUtils.Evaluate(methExp.Object);
        actOn = typeVal.Item2;
      }

      return ExecuteServiceCall(methExp.Method.IsStatic, remoteEndpointUri, actOn, methExp.Method.DeclaringType, methExp.Method.Name, parameters, rawAssembly);
    }

    /// <summary>
    /// Calls through to the server
    /// </summary>
    /// <param name="isStatic"></param>
    /// <param name="remoteEndpointUri"></param>
    /// <param name="actOn"></param>
    /// <param name="actOnType"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <param name="rawAssembly"></param>
    /// <returns></returns>
    protected ExecuteServiceCallResult ExecuteServiceCall(bool isStatic, Uri remoteEndpointUri, object actOn, Type actOnType, string methodName, IList<ParameterVariable> parameters, byte[] rawAssembly)
    {
      if (isStatic)
        return new NomadWcfClient().ExecuteStaticServiceCall(rawAssembly, remoteEndpointUri, NomadMethodType.Normal, actOnType, methodName, parameters);

      return new NomadWcfClient().ExecuteServiceCall(rawAssembly, remoteEndpointUri, NomadMethodType.Normal, actOn, methodName, parameters);
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
