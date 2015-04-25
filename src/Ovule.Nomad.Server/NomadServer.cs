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
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ovule.Nomad.Server
{
  /// <summary>
  /// An abstract implementation of INomadServer.  This handles pretty much everything that's required to run nomadic methods apart from 
  /// the network comms, which is left up to this classes derivatives.
  /// <see cref="Ovule.Nomad.Server.INomadServer"/>
  /// </summary>
  public abstract class NomadServer : INomadServer
  {
    /// TODO: Simulate parameters being passed by ref

    #region Properties/Fields

    private static ILogger _logger = LoggerFactory.Create(typeof(NomadServer).FullName);

    /// <summary>
    /// Ovule.Nomad.Processor bundles dependencies into a single Nomad assembly (to save having to ship files).
    /// Each dependency is stored as a resource with this prefix.
    /// </summary>
    private const string AssemblyResourceNamePrefix = "NomadRefRes:";

    /// <summary>
    /// Directory where nomadic assemblies are stored
    /// </summary>
    private const string DynamicNomadAssemblyRelativeDir = "dynomad";

    /// <summary>
    /// If the server does not have an assembly the client is making a request against then this string will be returned which 
    /// will let the client know it needs to send the raw assembly
    /// </summary>
    protected const string NomadAssemblyNotFoundReturnValue = "#______#NomadAssemblyNotFound#______#";

    private static Dictionary<string, Tuple<Assembly, string>> _execTypeAsmHash = new Dictionary<string, Tuple<Assembly, string>>();
    private static Dictionary<Tuple<string, string>, string> _asmPaths = new Dictionary<Tuple<string, string>, string>();

    #endregion Properties/Fields

    #region INomadService

    /// <summary>
    /// Executes the requested method.  It will set up the execution context using 'nonLocalVariables', call the method 
    /// passing in 'parameters' and return to the caller with a NomadicMethodResult.  The NomadicMethodResult contains the methods return value plus 
    /// current values for all non-local fields/properties that are referenced by the method, the method it calls, the methods they call, etc.
    /// </summary>
    /// <param name="methodType">The form of execution</param>
    /// <param name="assemblyFileName">The filename of the assembly that contains the method to execute, e.g. "MyFancyAssembly.dll"</param>
    /// <param name="assemblyFileHash">Used as a checksum to ensure both client and server are talking about the same assembly</param>
    /// <param name="typeFullName">The full name of the type that contains the method to execute, e.g. "MyFancyApplication.MyFancyType"</param>
    /// <param name="methodName">The name of the method to execute within Type 'typeFullName', e.g. "MyFancyMethod"</param>
    /// <param name="parameters">The parameters accepted by method 'methodName'. If the method does not require parameters then set as 'null'</param>
    /// <param name="nonLocalVariables">The non-local fields/properties that method 'methodName' (or any other method which 'methodName' calls) accesses</param>
    /// <returns>The results of executing method 'methodName', i.e. the methods return value and details of all non-local fields/properties that have been referenced/changed</returns>
    public virtual NomadMethodResult ExecuteNomadMethod(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables)
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

        _logger.LogInfo("ExecuteNomadMethod: methodType '{0}', assemblyFileName '{1}', typeFullName '{2}', methodName '{3}'",
          methodType, assemblyFileName, typeFullName, methodName);

        object executionObject = GetExecutionObject(assemblyFileName, assemblyFileHash, typeFullName);

        if (methodType == NomadMethodType.Normal)
          NonLocalReferenceHelper.SetNonLocalVariables(executionObject, executionObject.GetType(), nonLocalVariables);

        object methodResult = ExecuteMethod(executionObject.GetType(), methodType == NomadMethodType.Normal ? executionObject : null, methodName, parameters, assemblyFileHash);

        if (methodType == NomadMethodType.Normal)
          NonLocalReferenceHelper.RecoverNonLocalVariables(executionObject, nonLocalVariables);

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
    /// Executes the requested method.  It will set up the execution context using 'nonLocalVariables', call the method 
    /// passing in 'parameters' and return to the caller with a NomadicMethodResult.  The NomadicMethodResult contains the methods return value plus 
    /// current values for all non-local fields/properties that are referenced by the method, the method it calls, the methods they call, etc.
    /// 
    /// Same as other ExecuteNomadMethod(...) however accepts a raw assembly.  
    /// If the server is not aware of an assembly by name 'assemblyFilename' and with checksum 'assemblyFileHash' then it will expect the client 
    /// to pass it a copy, using this method.
    /// </summary>
    /// <param name="methodType">The form of execution</param>
    /// <param name="assemblyFileName">The filename of the assembly that contains the method to execute, e.g. "MyFancyAssembly.dll"</param>
    /// <param name="assemblyFileHash">Used as a checksum to ensure both client and server are talking about the same assembly</param>
    /// <param name="rawAssembly">The assembly (in binary form) to execute the method on</param>
    /// <param name="typeFullName">The full name of the type that contains the method to execute, e.g. "MyFancyApplication.MyFancyType"</param>
    /// <param name="methodName">The name of the method to execute within Type 'typeFullName', e.g. "MyFancyMethod"</param>
    /// <param name="parameters">The parameters accepted by method 'methodName'. If the method does not require parameters then set as 'null'</param>
    /// <param name="nonLocalVariables">The non-local fields/properties that method 'methodName' (or any other method which 'methodName' calls) accesses</param>
    /// <returns>The results of executing method 'methodName', i.e. the methods return value and details of all non-local fields/properties that have been referenced/changed</returns>
    public virtual NomadMethodResult ExecuteNomadMethod(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables)
    {
      SaveRawAssembly(rawAssembly, assemblyFileName, assemblyFileHash);
      return ExecuteNomadMethod(methodType, assemblyFileName, assemblyFileHash, typeFullName, methodName, parameters, nonLocalVariables);
    }

    /// <summary>
    /// This is the main method that does the work of actualy invoking the method requested by the client. It performs the invoke and returns the result. 
    /// It is expected that before this method is called the context in which the method will execute has been set up.
    /// </summary>
    /// <param name="executionType"></param>
    /// <param name="executionObject"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <param name="assemblyFileHash"></param>
    /// <returns>The result of executing the method the client requested</returns>
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
        MethodInfo executionMethod = executionType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
          null, methodParamTypes, null);

        if (executionMethod == null)
          throw new NomadException(string.Format("Failed to load method '{0}.{1}' from assembly '{2}'", executionType.FullName, methodName, asm.FullName));
        
        _logger.LogInfo("ExecuteMethod: Loaded method '{0}", methodName);

        object[] methParams = null;
        if (parameters != null)
          methParams = parameters.Select(v => v.Value).ToArray();

        object executionMethodResult = executionMethod.Invoke(executionObject, methParams);

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

    /// <summary>
    /// Takes a binary representation of an assembly and saves it
    /// </summary>
    /// <param name="rawAssembly"></param>
    /// <param name="assemblyFilename"></param>
    /// <param name="assemblyFileHash"></param>
    protected void SaveRawAssembly(byte[] rawAssembly, string assemblyFilename, string assemblyFileHash)
    {
      string asmDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DynamicNomadAssemblyRelativeDir, assemblyFileHash);

      if (!Directory.Exists(asmDir))
        Directory.CreateDirectory(asmDir);

      File.WriteAllBytes(Path.Combine(asmDir, assemblyFilename), rawAssembly);
    }

    /// <summary>
    /// Returns an array of all ParameterVariable types that are supplied
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>The types of the parameters in 'parameters'</returns>
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
    /// Creates an instance of the class which method execution will occur on and returns it.
    /// </summary>
    /// <param name="assemblyFileName"></param>
    /// <param name="assemblyFileHash"></param>
    /// <param name="typeFullName"></param>
    /// <returns>An instance of an object</returns>
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
    /// Returns an instance of the Type in 'assemblyFileName' with name 'typeFullName'
    /// </summary>
    /// <param name="assemblyFileName"></param>
    /// <param name="assemblyFileHash"></param>
    /// <param name="typeFullName"></param>
    /// <returns>An instance of the Type in 'assemblyFileName' with name 'typeFullName'</returns>
    protected Type GetExecutionType(string assemblyFileName, string assemblyFileHash, string typeFullName)
    {
      this.ThrowIfArgumentIsNoValueString(() => assemblyFileName);
      this.ThrowIfArgumentIsNoValueString(() => typeFullName);

      _logger.LogInfo("GetExecutionType: assemblyFileName = '{0}', typeFullName = '{1}'", assemblyFileName, typeFullName);

      //TODO: Load the assembly into it's own AppDomain so that it can be unloaded and then this exection doesn't need to be thrown
      Assembly asm = null;
      if (_execTypeAsmHash.ContainsKey(typeFullName))
      {
        Tuple<Assembly,string> asmHash = _execTypeAsmHash[typeFullName];
        if (asmHash.Item2 != assemblyFileHash)
          throw new NomadException("The assembly '{0}' has changed.  The server must be restarted in order to work with it", assemblyFileName);
        asm = asmHash.Item1;
      }

      if (asm == null)
      {
        string asmPath = GetAssemblyPath(assemblyFileName, assemblyFileHash);
        asm = Assembly.LoadFrom(asmPath);
        if (asm == null)
          throw new NomadException(string.Format("Failed to load assembly at path '{0}'", asmPath));
      }
      _logger.LogInfo("GetExecutionType: Assembly '{0}' loaded", asm.FullName);

      if (!_execTypeAsmHash.ContainsKey(typeFullName))
        _execTypeAsmHash.Add(typeFullName, new Tuple<Assembly, string>(asm, assemblyFileHash));

      Type executionType = asm.GetType(typeFullName);
      if (executionType == null)
        throw new NomadException(string.Format("Failed to load type '{0}' from assembly '{1}'", typeFullName, asm.FullName));
      _logger.LogInfo("GetExecutionType: Loaded type '{0}'", typeFullName);

      return executionType;
    }

    #endregion Util

    #region Reference Resolution

    /// <summary>
    /// Returns true if the assembly with name 'assemblyFilename' and checksum of 'assemblyFileHash' is found
    /// </summary>
    /// <param name="assemblyFilename"></param>
    /// <param name="assemblyFileHash"></param>
    /// <returns>True if the assembly with name 'assemblyFilename' and checksum of 'assemblyFileHash' is found</returns>
    protected bool IsRequiredAssemblyAvailable(string assemblyFilename, string assemblyFileHash)
    {
      string asmPath = GetAssemblyPath(assemblyFilename, assemblyFileHash, false);
      return !string.IsNullOrWhiteSpace(asmPath);
    }

    /// <summary>
    /// Returns the directories to consider when looking for assemblies that contain nomadic methods.
    /// Override in a derived class if you want different paths.
    /// </summary>
    /// <param name="assemblyFileHash"></param>
    /// <returns>The directories to consider when looking for assemblies that contain nomadic methods</returns>
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
    /// <param name="assemblyFilename"></param>
    /// <param name="assemblyFileHash"></param>
    /// <param name="throwFileNotFoundException"></param>
    /// <returns>The full path to assembly 'assemblyFilename' using the probing paths returned by GetAssemblyProbeDirectories(...)</returns>
    protected virtual string GetAssemblyPath(string assemblyFilename, string assemblyFileHash, bool throwFileNotFoundException = true)
    {
      Tuple<string, string> asmHash = new Tuple<string, string>(assemblyFilename, assemblyFileHash);
      if (_asmPaths.ContainsKey(asmHash))
        return _asmPaths[asmHash];

      foreach (string probeDir in GetAssemblyProbeDirectories(assemblyFileHash))
      {
        _logger.LogInfo("GetAssemblyPath: Trying to load assembly '{0}' with hash '{1}' from directory '{2}'", assemblyFilename, assemblyFileHash, probeDir);
        string probeFilename = Path.Combine(probeDir, assemblyFilename);
        if (File.Exists(probeFilename))
        {
          _logger.LogInfo("GetAssemblyPath: Found assembly '{0}' with hash '{1}' in directory '{2}'", assemblyFilename, assemblyFileHash, probeDir);
          _asmPaths.Add(new Tuple<string, string>(assemblyFilename, assemblyFileHash), probeFilename);
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
    /// When an assembly is processed at the client side any assemblies which the nomadic methods require 
    /// for thier execution are bundled up within the assembly as resources (so that these external libraries don't need 
    /// to be shipped seperatly to the server).  If the runtime can't resolve an assembly itself check if it's bundled as 
    /// a resource and if so load it into the process.
    /// </summary>
    /// <param name="resolveFrom"></param>
    /// <param name="assemblyName"></param>
    /// <param name="assemblyFileHash"></param>
    /// <returns>An instance of an Assembly 'assemblyName' if such an assembly exists as a resource in 'resolveFrom'</returns>
    protected Assembly TryResolveAssembly(Assembly resolveFrom, string assemblyName, string assemblyFileHash)
    {
      try
      {
        Assembly resolved = null;
        _logger.LogInfo("TryResolveAssembly: Processing request to resolve assembly '{0}'", assemblyName);

        //can happen in P2P (i.e. both sides have the same process name) setting where serialised type assembly isn't mapped to loaded assembly
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
