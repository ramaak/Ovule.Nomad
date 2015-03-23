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
using Mono.Cecil;
using Mono.Cecil.Cil;
using Ovule.Diagnostics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ovule.Nomad.Client
{
  /// <summary>
  /// An abstract implementation of INomadClient.  This does pretty much everything that's required of a Nomad Client other than network comms, 
  /// which is left up to derivatives of this class.
  /// 
  /// See documentation for Ovule.Nomad.Client.INomadClient
  /// 
  /// There are two settings which can be overridden withing the hosting applications configuration, i.e. App/Web.config "appSettings" section: 
  /// 
  /// IsNomadDisabled - the default value is "false".  
  /// Setting IsNomadDisabled to true will mean methods decorated with the [NomadicMethod] attribute will be executed completely on the client rather than the server.
  /// 
  /// NomadServerResponseTimeout - the default value is 00:00:30, i.e. 30 seconds.
  /// Set to 00:00:00 if you don't want there to be a timeout, however this is not advisable.
  /// 
  /// TODO: Simulate parameters being passed by ref
  /// </summary>
  public abstract class NomadClient : INomadClient
  {
    #region Properties/Fields

    private const string IsNomadDisabledConfig = "IsNomadDisabled";
    private const string NomadServerResponseTimeoutConfig = "NomadServerResponseTimeout";
    private const double DefaultNomadServerResponseTimeoutSeconds = 30;

    private static ILogger _logger = LoggerFactory.Create(typeof(NomadClient).FullName);

    protected static System.Configuration.Configuration NomadConfig { get; set; }
    protected static bool IsNomadDisabled { get; private set; }
    protected static TimeSpan NomadServerResponseTimeout { get; private set; }

    #endregion Properties/Fields

    #region ctors

    static NomadClient()
    {
      try
      {
        string configFileDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("{0}.dll", typeof(NomadClient).Assembly.GetName().Name));
        NomadConfig = ConfigurationManager.OpenExeConfiguration(configFileDll);

        string isDisabledString = NomadConfig.AppSettings.Settings[IsNomadDisabledConfig] == null ? null : NomadConfig.AppSettings.Settings[IsNomadDisabledConfig].Value;
        string responseTimeoutString = NomadConfig.AppSettings.Settings[NomadServerResponseTimeoutConfig] == null ? null : NomadConfig.AppSettings.Settings[NomadServerResponseTimeoutConfig].Value;

        IsNomadDisabled = false;
        if (!string.IsNullOrWhiteSpace(isDisabledString))
        {
          bool parsedBool = false;
          if (!bool.TryParse(isDisabledString, out parsedBool))
          {
            _logger.LogError("Invalid value for system setting '{0}' expected a boolean but value was '{1}'.  Setting is defaulting to 'False'",
              IsNomadDisabledConfig, isDisabledString);
          }
          else
            IsNomadDisabled = parsedBool;
        }

        NomadServerResponseTimeout = TimeSpan.FromSeconds(DefaultNomadServerResponseTimeoutSeconds);
        if (!string.IsNullOrWhiteSpace(responseTimeoutString))
        {
          TimeSpan parsedTimespan = TimeSpan.Zero;
          if (string.IsNullOrWhiteSpace(responseTimeoutString) || !TimeSpan.TryParse(responseTimeoutString, out parsedTimespan))
          {
            _logger.LogError("Invalid value for system setting '{0}' expected a TimeSpan but value was '{1}'.  Setting is defaulting to '{2}'",
              NomadServerResponseTimeoutConfig, responseTimeoutString, DefaultNomadServerResponseTimeoutSeconds);
          }
          else
            NomadServerResponseTimeout = parsedTimespan;
        }
        _logger.LogInfo("NomadClient constructed.  IsNomadDisabled = '{0}', NomadServerResponseTimeout = '{1}'", IsNomadDisabled, NomadServerResponseTimeout);
      }
      catch(Exception ex)
      {
        _logger.LogException(ex);
        throw;
      }
    }

    #endregion ctors

    #region Abstract

    /// <summary>
    
    /// </summary>
    /// <param name="assemblyName">The name of the assembly (without directory path) that contains the type to execute, e.g. "MyFancyAssembly.dll"</param>
    /// <param name="typeFullName">The name of the type that contains the method to execute, e.g. "MyFancyApplication.MyFancyType"</param>
    /// <param name="methodName">The name of the method with 'typeFullName' to execute on the server</param>
    /// <param name="parameters">The parameters to pass to method 'methodName', e.g. "MyFancyMethod"</param>
    /// <param name="nonLocalVariables">A collection of fields/properties that are currently with reach of method 'methodName', methods it calls, methods they call, etc.</param>
    /// <returns></returns>

    /// <summary>
    /// Implementation must send a request to a Nomad Server to execute a method, wait for the result and return it.
    /// </summary>
    /// <param name="endpoint">The Uri of the endpoint to execute the method on.  If this is null the default server will be used</param>
    /// <param name="methodType">The type of method to execute, i.e. Normal, Relay, Repeat, ...</param>
    /// <param name="runInMainThread">If true and attempt will be made to execute on the server applications main thread.  Useful in P2P scenarios where the method interacts with the GUI</param>
    /// <param name="assemblyName">The name of the assembly that contains the method to execute</param>
    /// <param name="typeFullName">The name of the type that contains the method to execute</param>
    /// <param name="methodName">The name of the method to execute</param>
    /// <param name="parameters">The parameters for the method to execute</param>
    /// <param name="nonLocalVariables">Collection of properties/fields that are accessed by the method to execute or other methods that can potentially enter the call stack</param>
    /// <returns></returns>
    protected abstract NomadMethodResult IssueServerRequest(Uri endpoint, NomadMethodType methodType, bool runInMainThread, string assemblyName, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables);

    #endregion Abstract

    #region INomadClient

    /// <summary>
    /// Call to initiate the sequence of events which will result in the Nomad Server executing method within the same 
    /// context that's currently on the client.  
    /// 
    /// 
    /// </summary>
    /// <param name="methodType">See documentation for Ovule.Nomad.NomadMethodType</param>
    /// <param name="runInMainThread">If true and attempt will be made to execute on the server applications main thread.  Useful in P2P scenarios where the method interacts with the GUI</param>
    /// <param name="actOn">The object of which type the method will execute on.  For static methods the method will not execute on the object but on the type.</param>
    /// <param name="methodName">The name of the method to execute on object/type taken from 'actOn' parameter</param>
    /// <param name="parameters">The parameters to pass to the method to execute</param>
    /// <returns></returns>
    public virtual ExecuteServiceCallResult ExecuteServiceCall(NomadMethodType methodType, bool runInMainThread, object actOn, string methodName, IList<ParameterVariable> parameters)
    {
      try
      {
        if (IsNomadDisabled)
          return new ExecuteServiceCallResult(false, null);

        this.ThrowIfArgumentIsNull(() => actOn);
        this.ThrowIfArgumentIsNoValueString(() => methodName);

        Type actOnType = actOn.GetType();

        _logger.LogInfo("ExecuteServiceCall: Transferring control of execution to external process for nomadic method '{0}.{1}'", actOn.GetType().FullName, methodName);

        string actOnAsmPath = actOnType.Assembly.CodeBase.Replace("file:///", "");

        IList<IVariable> nonLocalVaraibles = null;
        //currently it only makes sense to work on non-locals for normal exeuction methods. Relay methods don't impact upon the client 
        //and Repeat methods will have an impact anyway as they will run on the client.
        if (methodType == NomadMethodType.Normal)
          nonLocalVaraibles = GetCurrentNonLocalVariables(actOn, actOnAsmPath, methodName);

        //determine if the method be executed on the default server or if one was chosen at runtime
        Uri endpoint = GetEndpointDefinitionParameter(parameters);

        //hand over to subclass so it can do dirty work of issuing call to server
        NomadMethodResult result = IssueServerRequest(endpoint, methodType, runInMainThread, Path.GetFileName(actOnAsmPath), actOnType.FullName, methodName, parameters, nonLocalVaraibles);

        _logger.LogInfo("ExecuteServiceCall: Received response from Nomad server for method '{0}.{1}'", actOnType.FullName, methodName);

        //if the method type isn't NomadMethodType.Normal then result.NonLocalVariables should be null anyway.
        if (methodType == NomadMethodType.Normal && result.NonLocalVariables != null)
          NonLocalReferenceHelper.SetNonLocalVariables(actOn, result.NonLocalVariables);

        return new ExecuteServiceCallResult(true, result.ReturnValue);
      }
      catch (Exception ex)
      {
        _logger.LogException(ex);
        throw;
      }
    }

    /// <summary>
    /// Same as 'ExecuteServiceCall' however for static methods.
    /// 
    /// TODO: This probably isn't needed now.
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="runInMainThread"></param>
    /// <param name="actOnType"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public virtual ExecuteServiceCallResult ExecuteStaticServiceCall(NomadMethodType methodType, bool runInMainThread, Type actOnType, string methodName, IList<ParameterVariable> parameters)
    {
      try
      {
        if (IsNomadDisabled)
          return new ExecuteServiceCallResult(false, null);

        this.ThrowIfArgumentIsNull(() => actOnType);
        this.ThrowIfArgumentIsNoValueString(() => methodName);

        _logger.LogInfo("ExecuteStaticServiceCall: Transferring control of execution to external process for nomadic method '{0}.{1}'", actOnType.FullName, methodName);

        string actOnAsmPath = actOnType.Assembly.CodeBase.Replace("file:///", "");

        Uri endpoint = GetEndpointDefinitionParameter(parameters);
        NomadMethodResult result = IssueServerRequest(endpoint, methodType, runInMainThread, Path.GetFileName(actOnAsmPath), actOnType.FullName, methodName, parameters, null);

        _logger.LogInfo("ExecuteStaticServiceCall: Received response from Nomad server for method '{0}.{1}'", actOnType.FullName, methodName);

        return new ExecuteServiceCallResult(true, result.ReturnValue);
      }
      catch (Exception ex)
      {
        _logger.LogException(ex);
        throw;
      }
    }

    #endregion INomadClient

    #region General

    /// <summary>
    /// See documentation for IShippingContainer.
    /// 
    /// TODO: I'm not overly keep on IShippingContainer, consider alternatives
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    protected virtual Uri GetEndpointDefinitionParameter(IList<ParameterVariable> parameters)
    {
      if (parameters != null && parameters.Any())
      {
        foreach (ParameterVariable parameter in parameters)
        {
          if (parameter.Value is IShippingContainer)
          {
            Uri endpoint = ((IShippingContainer)parameter.Value).Destination;
            _logger.LogInfo("GetEndpointDefinitionParameter: '{0}' parameter found with destination endpoint of '{1}'", parameter.Value.GetType().FullName, endpoint);
            return endpoint;
          }
        }
      }
      return null;
    }

    #endregion General

    #region Non Local Variables

    /// <summary>
    /// Retuns a collection of variables which are within reach of 'methodName', the methods it calls, the methods they call, etc.  
    /// This collection contains not just the variable names but also their values.
    /// </summary>
    /// <param name="actOn"></param>
    /// <param name="actOnAsmPath"></param>
    /// <param name="methodName"></param>
    /// <returns></returns>
    protected IList<IVariable> GetCurrentNonLocalVariables(object actOn, string actOnAsmPath, string methodName)
    {
      this.ThrowIfArgumentIsNull(() => actOn);
      this.ThrowIfArgumentIsNoValueString(() => actOnAsmPath);
      this.ThrowIfArgumentIsNoValueString(() => methodName);

      _logger.LogInfo("GetCurrentNonLocalVariables: Acting on type '{0}', Method '{1}' from assembly at path '{2}'", actOn.GetType().FullName, methodName, actOnAsmPath);

      IList<IVariable> result = new List<IVariable>();
      Type actOnType = actOn.GetType();

      ModuleDefinition modDef = ModuleDefinition.ReadModule(actOnAsmPath);
      TypeDefinition typeDef = modDef.Types.FirstOrDefault(td => td.FullName == actOnType.FullName);
      if (typeDef != default(TypeDefinition))
      {
        MethodDefinition methDef = typeDef.Methods.FirstOrDefault(m => m.Name == methodName);
        if (methDef != null && methDef.Body != null)
          result = DiscoverCurrentNonLocalReferences(actOn, methDef);
      }
      return result;
    }

    /// <summary>
    /// Does the heavy lifting for GetCurrentNonLocalVariables(...).
    /// 
    /// TODO: Considerable scope for perfomance improvement here by caching the reference graph + tidy up code
    /// </summary>
    /// <param name="actOn"></param>
    /// <param name="methDef"></param>
    /// <param name="nonLocals"></param>
    /// <returns></returns>
    protected IList<IVariable> DiscoverCurrentNonLocalReferences(object actOn, MethodDefinition methDef, IDictionary<string, IVariable> nonLocals = null)
    {
      this.ThrowIfArgumentIsNull(() => actOn);
      this.ThrowIfArgumentIsNull(() => methDef);

      Type actOnType = actOn.GetType();
      if (nonLocals == null)
        nonLocals = new Dictionary<string, IVariable>();

      if (methDef != null && methDef.Body != null)
      {
        _logger.LogInfo("DiscoverCurrentNonLocalReferences: Discovering non local references in method '{0}' from type '{1}'", methDef.FullName, methDef.DeclaringType.FullName);

        foreach (Instruction instruction in methDef.Body.Instructions)
        {
          if (instruction.OpCode.OperandType == OperandType.InlineField)
          {
            FieldReference fr = (FieldReference)instruction.Operand;
            if (fr.DeclaringType == methDef.DeclaringType && !nonLocals.ContainsKey(fr.Name))
            {
              FieldInfo fi = actOnType.GetField(fr.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
              object[] ignoreAttribs = fi.GetCustomAttributes(typeof(NomadIgnoreAttribute), false);
              if (ignoreAttribs != null && ignoreAttribs.Any())
              {
                _logger.LogInfo("DiscoverCurrentNonLocalReferences: Ignoring field '{0}' due to it being decorated with '{1}'", fr.Name, typeof(NomadIgnoreAttribute).FullName);
                continue;
              }
              if (fi == null)
                throw new NomadVariableException("Did not find field '{0}' referenced by method '{1}' on type '{2}'", fr.Name, methDef.FullName, actOnType.FullName);
              FieldVariable fieldVar = new FieldVariable(fr.Name, fi.FieldType, fi.GetValue(actOn));
              nonLocals.Add(fr.Name, fieldVar);

              _logger.LogInfo("DiscoverCurrentNonLocalReferences: Field '{0}' is referenced in method '{1}, current value is '{2}'", fieldVar.Name, methDef.FullName, fieldVar.Value == null ? "null" : fieldVar.Value);
            }
          }
          else if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodDefinition)
          {
            MethodDefinition callMethDef = (MethodDefinition)instruction.Operand;
            if (callMethDef.IsGetter || callMethDef.IsSetter)
            {
              string propMethName = ((MethodDefinition)instruction.Operand).Name;
              if (propMethName.StartsWith("get_") || propMethName.StartsWith("set_"))
              {
                string propName = propMethName.Substring(4);
                if (!nonLocals.ContainsKey(propName))
                {
                  PropertyInfo pi = actOnType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                  object[] ignoreAttribs = pi.GetCustomAttributes(typeof(NomadIgnoreAttribute), false);
                  if (ignoreAttribs != null && ignoreAttribs.Any())
                  {
                    _logger.LogInfo("DiscoverCurrentNonLocalReferences: Ignoring property '{0}' due to it being decorated with '{1}'", propName, typeof(NomadIgnoreAttribute).FullName);
                    continue;
                  }
                  if (pi == null)
                    throw new FieldAccessException(string.Format("Did not find property '{0}' referenced by method '{1}' on type '{2}'", pi.Name, methDef.FullName, actOnType.FullName));
                  PropertyVariable propVar = new PropertyVariable(pi.Name, pi.PropertyType, pi.GetValue(actOn, null));
                  nonLocals.Add(propName, propVar);

                  _logger.LogInfo("DiscoverCurrentNonLocalReferences: Property '{0}' is referenced in method '{1}, current value is '{2}'", propVar.Name, methDef.FullName, propVar.Value == null ? "null" : propVar.Value);
                }
              }
            }
            else if (callMethDef.DeclaringType.Equals(methDef.DeclaringType))
            {
              _logger.LogInfo("DiscoverCurrentNonLocalReferences: Method '{0}' is called from '{1}' moving on to discover references within called method", callMethDef.FullName, methDef.FullName);
              IList<IVariable> innerRefs = DiscoverCurrentNonLocalReferences(actOn, callMethDef, nonLocals);
              if (innerRefs != null && innerRefs.Any())
              {
                foreach (IVariable innerRef in innerRefs)
                {
                  if (!nonLocals.ContainsKey(innerRef.Name))
                    nonLocals.Add(innerRef.Name, innerRef);
                }
              }
            }
          }
        }
      }
      return nonLocals.Values.ToList();
    }

    #endregion Non Local Variables
  }
}