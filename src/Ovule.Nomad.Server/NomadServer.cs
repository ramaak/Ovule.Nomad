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
using Ovule.Diagnostics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;

namespace Ovule.Nomad.Server
{
  /// <summary>
  /// An abstract implementation of INomadServer.  This handles pretty much everything that's required to run nomadic methods apart from 
  /// the network comms, which is left up to this classes derivatives.
  /// 
  /// See documentation for Ovule.Nomad.Server.INomadServer
  /// 
  /// TODO: Simulate parameters being passed by ref
  /// TODO: Tidy up, inc. sorting out horrible method naming!
  /// </summary>
  public abstract class NomadServer : INomadServer
  {
    #region Properties/Varaibles
    
    /// <summary>
    /// Ovule.Nomad.Processor bundles dependencies into a single Nomad assembly (to save having to ship files).
    /// Each dependency is stored as a resource with this prefix.
    /// </summary>
    private const string AssemblyResourceNamePrefix = "NomadRefRes:";
    private const string DynamicNomadAssemblyRelativeDir = "dynomad";
    protected const string NomadAssemblyNotFoundReturnValue = "#______#NomadAssemblyNotFound#______#";

    private static ILogger _logger = LoggerFactory.Create(typeof(NomadServer).FullName);

    protected static System.Configuration.Configuration NomadConfig { get; private set; }

    #endregion Properties/Varaibles

    #region ctors

    static NomadServer()
    {
      string configFileDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("{0}.dll", typeof(INomadServer).Assembly.GetName().Name));
      NomadConfig = ConfigurationManager.OpenExeConfiguration(configFileDll);
    }

    #endregion ctors

    #region INomadService

    /// <summary>
    /// Refer to comments for Ovule.Nomad.Server.INomadServer.ExecuteNomadicMethod(...)
    /// </summary>
    /// <param name="assemblyFileName"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <param name="nonLocalVariables"></param>
    /// <returns></returns>
    public virtual NomadMethodResult ExecuteNomadMethod(NomadMethodType methodType, bool runInMainThread, string assemblyFileName, string assemblyFileHash, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables)
    {
      //TODO: make more efficient, this will trigger a search on the probing path and there will be another later.  Ensure just one search
      if (!IsRequiredAssemblyAvailable(assemblyFileName, assemblyFileHash))
      {
        _logger.LogWarning("Could not find assembly '{0}' on probing path.  Letting client know so that it may make another request which provides the raw assembly", assemblyFileName);
        return new NomadMethodResult(NomadAssemblyNotFoundReturnValue, null);
      }

      try
      {
        this.ThrowIfArgumentIsNoValueString(() => assemblyFileName);
        this.ThrowIfArgumentIsNoValueString(() => typeFullName);
        this.ThrowIfArgumentIsNoValueString(() => methodName);

        _logger.LogInfo("ExecuteNomadMethod: methodType '{0}', runInMainThread '{1}', assemblyFileName '{2}', typeFullName '{3}', methodName '{4}'",
          methodType, runInMainThread, assemblyFileName, typeFullName, methodName);

        object methodResult = null;
        if (runInMainThread)
          throw new NotSupportedException("This feature isn't yet supported");
        else
          methodResult = ExecuteNomadMethodInRequiredThread(methodType, assemblyFileName, assemblyFileHash, typeFullName, methodName, parameters, nonLocalVariables);

        _logger.LogInfo("ExecuteNomadicMethod: Complete");
        NomadMethodResult result = new NomadMethodResult(methodResult, nonLocalVariables);
        return result;
      }
      catch (Exception ex)
      {
        _logger.LogException(ex);
        throw;
      }
    }

    /// <summary>
    /// Refer to comments for Ovule.Nomad.Server.INomadServer.ExecuteNomadicMethod(...)
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="runInMainThread"></param>
    /// <param name="assemblyFileName"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <param name="nonLocalVariables"></param>
    /// <returns></returns>
    public virtual NomadMethodResult ExecuteNomadMethod(NomadMethodType methodType, bool runInMainThread, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables)
    {
      SaveRawAssembly(rawAssembly, assemblyFileName, assemblyFileHash);
      return ExecuteNomadMethod(methodType, runInMainThread, assemblyFileName, assemblyFileHash, typeFullName, methodName, parameters, nonLocalVariables);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="assemblyFileName"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <param name="nonLocalVariables"></param>
    /// <returns></returns>
    private object ExecuteNomadMethodInRequiredThread(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables)
    {
      object executionObject = GetExecutionObject(assemblyFileName, assemblyFileHash, typeFullName);

      if (methodType == NomadMethodType.Normal)
        NonLocalReferenceHelper.SetNonLocalVariables(executionObject, executionObject.GetType(), nonLocalVariables);

      object methodResult = ExecuteMethod(executionObject.GetType(), methodType == NomadMethodType.Normal ? executionObject : null, methodName, parameters, assemblyFileHash);

      if (methodType == NomadMethodType.Normal)
        NonLocalReferenceHelper.RecoverNonLocalVariables(executionObject, nonLocalVariables);
      return methodResult;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executionType"></param>
    /// <param name="executionObject">null for static calls</param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    private object ExecuteMethod(Type executionType, object executionObject, string methodName, IList<ParameterVariable> parameters, string assemblyFileHash)
    {
      this.ThrowIfArgumentIsNull(() => executionType);
      this.ThrowIfArgumentIsNoValueString(() => methodName);

      _logger.LogInfo("ExecuteMethod: executionObject = '{0}', methodName = '{1}'", executionType.FullName, methodName);

      Assembly asm = executionType.Assembly;
      ResolveEventHandler onAssemblyResolve = (s, args) => { return TryResolveAssembly(asm, args.Name, assemblyFileHash); };
      AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;
      try
      {
        Type[] methodParamTypes = GetMethodParameterTypes(parameters);
        FlickIsRepeatParameterIfRequired(parameters);

        MethodInfo executionMethod = executionType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
          null, methodParamTypes, null);

        if (executionMethod == null)
          throw new NomadException(string.Format("Failed to load method '{0}.{1}' from assembly '{2}'", executionType.FullName, methodName, asm.FullName));
        
        _logger.LogInfo("ExecuteMethod: Loaded method '{0}", methodName);

        object[] methParams = null;
        if (parameters != null)
          methParams = parameters.Select(v => v.Value).ToArray();

        object executionMethodResult = executionMethodResult = executionMethod.Invoke(executionObject, methParams);

        _logger.LogInfo("ExecuteMethod: Returned from method '{0}.{1}'", executionType.FullName, methodName);

        _logger.LogInfo("ExecuteMethod: Complete");
        return executionMethodResult;
      }
      finally
      {
        AppDomain.CurrentDomain.AssemblyResolve -= onAssemblyResolve;
      }
    }

    #endregion INomadService

    #region Util

    protected void SaveRawAssembly(byte[] rawAssembly, string assemblyFilename, string assemblyFileHash)
    {
      string asmDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DynamicNomadAssemblyRelativeDir, assemblyFileHash);

      if (!Directory.Exists(asmDir))
        Directory.CreateDirectory(asmDir);

      File.WriteAllBytes(Path.Combine(asmDir, assemblyFilename), rawAssembly);
    }

    /// <summary>
    /// If this is a non-Normal method (i.e. Relay, Repeat, ...) a string parameter will have been added to the method by the Processor and 
    /// the method will react to the value of this.  Since we're now on the server we want to flick this value so that the method executes 
    /// in "server mode"
    /// </summary>
    /// <param name="parameters"></param>
    private void FlickIsRepeatParameterIfRequired(IList<ParameterVariable> parameters)
    {
      if (parameters != null && parameters.Count > 0)
      {
        //TODO: Change this so the parameter is of a particular type rather than looking at it's value
        ParameterVariable potentialIsRepeatVar = parameters[parameters.Count - 1];
        if (potentialIsRepeatVar != null && Constants.IsRepeatMethodCallFalseValue.Equals(potentialIsRepeatVar.Value))
          parameters[parameters.Count - 1].Value = Constants.IsRepeatMethodCallTrueValue;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    private Type[] GetMethodParameterTypes(IList<ParameterVariable> parameters)
    {
      Type[] methodParamTypes = null;
      if (parameters != null)
      {
        List<Type> methodParamTypesList = new List<Type>();
        foreach (ParameterVariable parameter in parameters)
        {
          Type parameterType = Type.GetType(parameter.Value.GetType().AssemblyQualifiedName);
          if (parameterType == null)
            throw new TypeLoadException(string.Format("Parameter '{0}' is of type '{1}' however the type could not be loaded", parameter.Name, parameter.TypeFullName));

          methodParamTypesList.Add(parameterType);
        }
        methodParamTypes = methodParamTypesList.ToArray();
      }
      else
        methodParamTypes = new Type[0];
      return methodParamTypes;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assemblyFileName"></param>
    /// <param name="typeFullName"></param>
    /// <returns></returns>
    protected object GetExecutionObject(string assemblyFileName, string assemblyFileHash, string typeFullName)
    {
      Type executionType = GetExecutionType(assemblyFileName, assemblyFileHash, typeFullName);

      object executionObject = Activator.CreateInstance(executionType);
      if (executionObject == null)
        throw new NomadException(string.Format("Failed to create instance of type '{0}' from assembly '{1}'", typeFullName, assemblyFileName));
      _logger.LogInfo("GetExecutionObject: Created instance of type '{0}'", typeFullName);

      return executionObject;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assemblyFileName"></param>
    /// <param name="typeFullName"></param>
    /// <returns></returns>
    private Type GetExecutionType(string assemblyFileName, string assemblyFileHash, string typeFullName)
    {
      this.ThrowIfArgumentIsNoValueString(() => assemblyFileName);
      this.ThrowIfArgumentIsNoValueString(() => typeFullName);

      _logger.LogInfo("GetExecutionType: assemblyFileName = '{0}', typeFullName = '{1}'", assemblyFileName, typeFullName);

      string asmPath = GetAssemblyPath(assemblyFileName, assemblyFileHash);
      Assembly asm = Assembly.LoadFrom(asmPath);
      if (asm == null)
        throw new NomadException(string.Format("Failed to load assembly at path '{0}'", asmPath));
      _logger.LogInfo("GetExecutionType: Assembly '{0}' loaded", asm.FullName);


      Type executionType = asm.GetType(typeFullName);
      if (executionType == null)
        throw new NomadException(string.Format("Failed to load type '{0}' from assembly '{1}'", typeFullName, asm.FullName));
      _logger.LogInfo("GetExecutionType: Loaded type '{0}'", typeFullName);

      return executionType;
    }

    #endregion Util

    #region Reference Resolution

    /// <summary>
    /// Returns true if the assembly with name 'assemblyFilename' is found on the probing path
    /// </summary>
    /// <param name="assemblyFilename"></param>
    /// <returns></returns>
    protected bool IsRequiredAssemblyAvailable(string assemblyFilename, string assemblyFileHash)
    {
      string asmPath = GetAssemblyPath(assemblyFilename, assemblyFileHash, false);
      return !string.IsNullOrWhiteSpace(asmPath);
    }

    /// <summary>
    /// Returns the directories to consider when looking for assemblies that contain nomadic methods.
    /// Override in a derived class if you want different paths.
    /// </summary>
    /// <returns></returns>
    protected virtual string[] GetAssemblyProbeDirectories(string assemblyFileHash)
    {
      string[] probeDirs = 
      {
        AppDomain.CurrentDomain.BaseDirectory,
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"bin"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,DynamicNomadAssemblyRelativeDir, assemblyFileHash)
      };
      return probeDirs;
    }

    /// <summary>
    /// Returns the full path to assembly 'assemblyFilename' using the probing paths returned by GetAssemblyProbeDirectories(...). 
    /// If the assembly is not found a FileNotFoundException is thrown.
    /// </summary>
    /// <param name="assemblyFilename">The filename of the assembly that a full path is needed for, e.g. "MyFancyAssembly.dll"</param>
    /// <returns></returns>
    protected virtual string GetAssemblyPath(string assemblyFilename, string assemblyFileHash, bool throwFileNotFoundException = true)
    {
      foreach (string probeDir in GetAssemblyProbeDirectories(assemblyFileHash))
      {
        _logger.LogInfo("GetAssemblyPath: Trying to load assembly '{0}' with hash '{1}' from directory '{2}'", assemblyFilename, assemblyFileHash, probeDir);
        string probeFilename = Path.Combine(probeDir, assemblyFilename);
        if (File.Exists(probeFilename))
        {
          _logger.LogInfo("GetAssemblyPath: Found assembly '{0}' with hash '{1}' in directory '{2}'", assemblyFilename, assemblyFileHash, probeDir);
          return probeFilename;
        }
      }

      if (throwFileNotFoundException)
      {
        throw new FileNotFoundException(string.Format("GetAssemblyPath: Could not find assembly '{0}'. " +
          "Perhaps '{1}.GetAssemblyProbeDirectories()' or '{1}.GetAssemblyPath(string)' should be overloaded",
          assemblyFilename, this.GetType().FullName));
      }
      else
        return null;
    }

    /// <summary>
    /// When an assembly is processed (by Ovule.Nomad.Processor) any assemblies which the nomadic methods require 
    /// for thier execution are bundled up within the assembly as resources (so that these external libraries don't need 
    /// to be shipped seperatly to the server).  If the runtime can't resolve an assembly itself check if it's bundled as 
    /// a resource and if so load it into the process.
    /// </summary>
    /// <param name="resolveFrom"></param>
    /// <param name="assemblyName"></param>
    /// <returns></returns>
    protected Assembly TryResolveAssembly(Assembly resolveFrom, string assemblyName, string assemblyFileHash)
    {
      try
      {
        Assembly resolved = null;
        _logger.LogInfo("TryResolveAssembly: Processing request to resolve assembly '{0}'", assemblyName);

        //can happen in P2P setting where serialised type assembly isn't mapped to loaded assembly
        if (assemblyName == resolveFrom.FullName)
          resolved = resolveFrom;
        else
        {
          string asmResourceName = string.Format("{0}{1}", AssemblyResourceNamePrefix, assemblyName);
          string[] resourceNames = resolveFrom.GetManifestResourceNames();
          if (resourceNames != null && resourceNames.Contains(asmResourceName))
          {
            using (Stream stream = resolveFrom.GetManifestResourceStream(asmResourceName))
            {
              byte[] assemblyData = new byte[stream.Length];
              stream.Read(assemblyData, 0, assemblyData.Length);

              AssemblyName asmName = new AssemblyName(assemblyName);
              string asmPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DynamicNomadAssemblyRelativeDir, assemblyFileHash, string.Format("{0}.dll", asmName.Name));
              File.WriteAllBytes(asmPath, assemblyData);

              resolved = Assembly.LoadFile(asmPath);

              _logger.LogInfo("TryResolveAssembly: Resolved assembly '{0}'", resolved.FullName);
            }
          }
        }
        if(resolved == null)
          _logger.LogInfo("TryResolveAssembly: Did not resolve assembly '{0}' returing control to .Net framework.", assemblyName);
        
        return resolved;
      }
      catch(Exception ex)
      {
        _logger.LogException(ex, "TryResolveAssembly: Failed to resolve assembly '{0}' from resources in '{1}", assemblyName, resolveFrom.FullName);
        throw;
      }
    }
    
    #endregion Reference Resolution
  }
}
