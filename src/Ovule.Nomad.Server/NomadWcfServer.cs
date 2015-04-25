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
using Ovule.Nomad.Wcf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;

namespace Ovule.Nomad.Server
{
  /// <summary>
  /// A WCF entry point to <see cref="Ovule.Nomad.Server.NomadServer"/>.  
  /// This is purely adding a WCF layer above NomadServer.  It catches traffic and moves it down into NomadServer.
  /// 
  /// In terms of security this class is taking a "relaxed by default" approach however it is possible to override this by 
  /// creating an app/web.config file and defining your own WCF configuration.  
  /// 
  /// N.B. NomadServer itself does not know nor care about the transport mechanism and can be used with any form of network comms.  
  /// </summary>
  public class NomadWcfServer : NomadServer, INomadWcfService, IDisposable
  {
    #region Properties/Fields

    private static ILogger _logger = LoggerFactory.Create(typeof(NomadWcfServer).FullName);

    private static Uri _serviceEndpointUri;
    public ServiceHost ServiceHost { get; private set; }

    #endregion Properties/Fields

    #region ctors

    /// <summary>
    /// Constructs an instance of the service which is configured through an application configuration file.
    /// </summary>
    public NomadWcfServer()
    {
      _logger.LogInfo("Constructing without providing endpoint definition, therefore relying on application WCF configuration");
    }

    /// <summary>
    /// Constructs an instance of the service which is accessible at the URI 'endpointUri'.  If using this constructor 
    /// then it is not a requirment to have an application configuration file however it is still allowed.
    /// </summary>
    /// <param name="endpointUri">The URI the server will be accessible from</param>
    public NomadWcfServer(Uri endpointUri)
    {
      this.ThrowIfArgumentIsNull(() => endpointUri);

      _serviceEndpointUri = endpointUri;
      _logger.LogInfo("Constructing with URI of '{0}'", endpointUri);
    }

    #endregion ctors

    #region Self Hosting

    /// <summary>
    /// Moves the process into a state where it's listening for WCF connections.
    /// 
    /// Only call this if self-hosting the service.  If hosted within IIS then never need to call this method.
    /// </summary>
    public void Start()
    {
      try
      {
        _logger.LogInfo("Start: Starting self-hosted service");

#if __MonoCS__
        //Mono has incomplete WCF implementation so have to do some things differently
        MonoConfigure();
#else
        //will trigger call to Configure(...)
        if (_serviceEndpointUri == null)
          ServiceHost = new ServiceHost(typeof(NomadWcfServer)); //relying on WCF configuration in app/web.config
        else
          ServiceHost = new ServiceHost(typeof(NomadWcfServer), _serviceEndpointUri);
#endif

        ServiceHost.Open();

        _logger.LogInfo("Start: Started self-hosted service");
      }
      catch (Exception ex)
      {
        _logger.LogException(ex);
        Stop();
        throw;
      }
    }

    /// <summary>
    /// Moves the process into a state where it stops listening for WCF connections.
    /// 
    /// Only call this if self-hosting the service.  If hosted within IIS then never need to call this method
    /// </summary>
    public void Stop()
    {
      try
      {
        _logger.LogInfo("Stop: Stopping listener at endpoint '{0}'", _serviceEndpointUri);

        if (ServiceHost != null && ServiceHost.State != CommunicationState.Closed)
          ServiceHost.Close();
        ServiceHost = null;

        _logger.LogInfo("Stop: Stopped listener at endpoint '{0}'", _serviceEndpointUri);
      }
      catch (Exception ex)
      {
        _logger.LogException(ex);
        throw;
      }
    }

    #endregion Self Hosting

    #region Configuration

#if __MonoCS__
    protected void MonoConfigure()
    {
      Binding binding = null;

      if (_serviceEndpointUri == null)
        throw new NomadServerInitialisationException("Neither a basic service endpoint URI was defined or any WCF configuration. " +
          "Need at least one of these to be specified in the application configuration");

      ServiceHost = new ServiceHost(typeof(NomadWcfServer));
      UriType uriType = UriUtils.GetType(_serviceEndpointUri);
      if (uriType == UriType.Http)
      {
        binding = new BasicHttpBinding()
        {
          MaxBufferPoolSize = int.MaxValue,
          MaxReceivedMessageSize = int.MaxValue,
          ReaderQuotas = XmlDictionaryReaderQuotas.Max
        };
      }
      else if (uriType == UriType.Tcp)
      {
        binding = new NetTcpBinding()
        {
          MaxBufferPoolSize = int.MaxValue,
          MaxReceivedMessageSize = int.MaxValue,
          ReaderQuotas = XmlDictionaryReaderQuotas.Max

        };
      }
      else if (uriType == UriType.NamedPipe)
      {
        binding = new NetNamedPipeBinding()
        {
          MaxBufferPoolSize = int.MaxValue,
          MaxReceivedMessageSize = int.MaxValue,
          ReaderQuotas = XmlDictionaryReaderQuotas.Max

        };
      }
      else
        throw new NomadServerInitialisationException(string.Format("Unexpected endpoint type of '{0}'", uriType.ToString()));

      ServiceHost.AddServiceEndpoint(typeof(INomadWcfService), binding, _serviceEndpointUri);
    }
#endif

#if !__MonoCS__
    /// <summary>
    /// Checks for an application configuration file and if one is found the service is configured according to this file.
    /// </summary>
    /// <param name="config"></param>
    /// <returns>True if the service is configured through an application configuration file</returns>
    protected static bool TryLoadCustomConfiguration(ServiceConfiguration config)
    {
      string configFileDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("{0}.dll", typeof(INomadServer).Assembly.GetName().Name));
      System.Configuration.Configuration nomadConfig = ConfigurationManager.OpenExeConfiguration(configFileDll);

      ServicesSection servicesSection = nomadConfig.GetSection("system.serviceModel/services") as ServicesSection;
      bool isConfig = servicesSection != null && servicesSection.Services != null && servicesSection.Services.Count > 0;
      if (isConfig)
        config.LoadFromConfiguration(nomadConfig);
      return isConfig;
    }

    /// <summary>
    /// Configures the service.  First TryLoadCustomConfiguration(...) is called and if this returns false 
    /// a default binding is created (based on the service URI).  The default configuration is relaxed in terms of 
    /// security and allowances.  The maximum amount of data allowed across a WCF connection is permitted and there is 
    /// no security.  Timeouts are left as the WCF defaults.  
    /// 
    /// If you do not want to accept the chosen defaults then create an application configuration file which contains 
    /// WCF configuration.
    /// </summary>
    /// <param name="config"></param>
    public static void Configure(ServiceConfiguration config)
    {
      try
      {
        if (!TryLoadCustomConfiguration(config))
        {
          _logger.LogInfo("Configure: No custom configuration found, using default configuration");

          if (_serviceEndpointUri == null)
            throw new NomadServerInitialisationException("Neither a basic service endpoint URI was defined or any WCF configuration. " +
              "Need at least one of these to be specified in the application configuration");

          UriType uriType = UriUtils.GetType(_serviceEndpointUri);
          if (uriType == UriType.Http)
          {
            BasicHttpBinding binding = new BasicHttpBinding() { MaxBufferPoolSize = int.MaxValue, MaxReceivedMessageSize = int.MaxValue };
            binding.Security.Mode = BasicHttpSecurityMode.None;
            config.EnableProtocol(binding);
            config.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
          }
          else if (uriType == UriType.Tcp)
          {
            NetTcpBinding binding = new NetTcpBinding() { MaxBufferPoolSize = int.MaxValue, MaxReceivedMessageSize = int.MaxValue };
            binding.Security.Mode = SecurityMode.None;
            config.EnableProtocol(binding);
          }
          else if (uriType == UriType.NamedPipe)
          {
            NetNamedPipeBinding binding = new NetNamedPipeBinding() { MaxBufferPoolSize = int.MaxValue, MaxReceivedMessageSize = int.MaxValue };
            binding.Security.Mode = NetNamedPipeSecurityMode.None;
            config.EnableProtocol(binding);
          }
          else
            throw new NomadServerInitialisationException(string.Format("Unexpected endpoint type of '{0}'", uriType.ToString()));

          config.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
        }
      }
      catch(Exception ex)
      {
        _logger.LogException(ex);
        throw;
      }
    }
#endif
    #endregion Configuration

    #region OperationContract

    /// <summary>
    /// This method just passes control through to <see cref="Ovule.Nomad.Server.NomadServer.ExecuteNomadMethod"/>
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
    public NomadMethodResult ExecuteNomadMethodRaw(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables)
    {
      return ExecuteNomadMethod(methodType, assemblyFileName, assemblyFileHash, rawAssembly, typeFullName, methodName, parameters, nonLocalVariables);
    }

    /// <summary>
    /// If no implementation of <see cref="Ovule.Nomad.Wcf.IWcfKnowTypeProvider"/> is available then the DataContractSerializer cannot be used 
    /// to serialise data travelling between client and server.  The system will fall back to the BinaryFormatter which can format pretty much 
    /// and type - however this is slower than the DataContractSerialiser.
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="assemblyFileName"></param>
    /// <param name="assemblyFileHash"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="serialisedParameters"></param>
    /// <param name="serialisedNonLocalVariables"></param>
    /// <returns>A base 64 encoded string which is a binary serialised <see cref="Ovule.Nomad.NomadMethodResult"/></returns>
    public string ExecuteNomadMethodUsingBinarySerialiser(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, string typeFullName, string methodName, IList<string> serialisedParameters, IList<string> serialisedNonLocalVariables)
    {
      ResolveEventHandler onAssemblyResolve = null;
      try
      {
        NomadMethodResult result = null;
        Serialiser serialiser = new Serialiser();

        //TODO: make more efficient, this will trigger a search on the probing path and there will be another later.  Ensure just one search
        if (!IsRequiredAssemblyAvailable(assemblyFileName, assemblyFileHash))
        {
          _logger.LogWarning("Could not find assembly '{0}' on probing path.  Letting client know so that it may make another request which provides the raw assembly", assemblyFileName);
          result = new NomadMethodResult(NomadAssemblyNotFoundReturnValue, null);
        }
        else
        {
          //the act of deserialising parameters and variables may trigger an assembly load
          Type executionType = GetExecutionType(assemblyFileName, assemblyFileHash, typeFullName);
          Assembly asm = executionType.Assembly;
          onAssemblyResolve = (s, args) => { return TryResolveAssembly(asm, args.Name, assemblyFileHash); };
          AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;

          //TODO: Experiment with this to see if serialising individual items is better/worse than serialising lists as a whole
          IList<ParameterVariable> parameters = serialiser.DeserialiseBase64<ParameterVariable>(serialisedParameters, false);
          IList<IVariable> nonLocalVariables = serialiser.DeserialiseBase64<IVariable>(serialisedNonLocalVariables, true);

          result = base.ExecuteNomadMethod(methodType, assemblyFileName, assemblyFileHash, typeFullName, methodName, parameters, nonLocalVariables);
        }

        if (result == null)
          throw new NullReferenceException("The result of calling ExecuteNomadMethod(...) was 'null'");

        string serialisedResult = serialiser.SerialiseToBase64(result);
        return serialisedResult;
      }
      catch (Exception ex)
      {
        _logger.LogException(ex);
        throw;
      }
      finally
      {
        if (onAssemblyResolve != null)
          AppDomain.CurrentDomain.AssemblyResolve -= onAssemblyResolve;
      }
    }

    /// <summary>
    /// If no implementation of <see cref="Ovule.Nomad.Wcf.IWcfKnowTypeProvider"/> is available then the DataContractSerializer cannot be used 
    /// to serialise data travelling between client and server.  The system will fall back to the BinaryFormatter which can format pretty much 
    /// and type - however this is slower than the DataContractSerialiser.
    /// 
    /// If the server does not know about the assembly the client requires functionality from then this method must be used so that the assembly 
    /// can be delivered via the 'rawAssembly' parameter.
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="assemblyFileName"></param>
    /// <param name="assemblyFileHash"></param>
    /// <param name="rawAssembly"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="serialisedParameters"></param>
    /// <param name="serialisedNonLocalVariables"></param>
    /// <returns>A base 64 encoded string which is a binary serialised <see cref="Ovule.Nomad.NomadMethodResult"/></returns>
    public string ExecuteNomadMethodUsingBinarySerialiserRaw(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<string> serialisedParameters, IList<string> serialisedNonLocalVariables)
    {
      SaveRawAssembly(rawAssembly, assemblyFileName, assemblyFileHash);
      return ExecuteNomadMethodUsingBinarySerialiser(methodType, assemblyFileName, assemblyFileHash, typeFullName, methodName, serialisedParameters, serialisedNonLocalVariables);
    }

    #endregion OperationContract

    #region IDisposable

    /// <summary>
    /// The equivilent of <see cref="Stop"/>
    /// </summary>
    public void Dispose()
    {
      if (ServiceHost != null && (ServiceHost.State == CommunicationState.Opening || ServiceHost.State == CommunicationState.Opened))
        Stop();
    }

    #endregion IDisposable
  }
}
