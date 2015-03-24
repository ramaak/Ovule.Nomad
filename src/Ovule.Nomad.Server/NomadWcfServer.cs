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
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;

namespace Ovule.Nomad.Server
{
  /// <summary>
  /// A WCF entry point to NomadServer.  This type doesn't need to do anything other than be an implementation of INomadWcfService.
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
    /// 
    /// </summary>
    public NomadWcfServer()
    {
      _logger.LogInfo("Constructing without providing endpoint definition, therefore relying on application WCF configuration");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="endpointDefinition"></param>
    public NomadWcfServer(Uri endpointUri)
    {
      this.ThrowIfArgumentIsNull(() => endpointUri);

      _serviceEndpointUri = endpointUri;
      _logger.LogInfo("Constructing with URI of '{0}'", endpointUri);
    }

    #endregion ctors

    #region Self Hosting

    /// <summary>
    /// Only call this if self-hosting the service.  If hosted within IIS then never need to call this method.
    /// </summary>
    public void Start()
    {
      try
      {
        _logger.LogInfo("Start: Starting self-hosted service");

        //will trigger call to Configure(...)
        if (_serviceEndpointUri == null)
          ServiceHost = new ServiceHost(typeof(NomadWcfServer)); //relying WCF configuration in app/web.config
        else
          ServiceHost = new ServiceHost(typeof(NomadWcfServer), _serviceEndpointUri);

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

    protected static bool TryLoadCustomConfiguration(ServiceConfiguration config)
    {
      ServicesSection servicesSection = NomadConfig.GetSection("system.serviceModel/services") as ServicesSection;
      bool isConfig = servicesSection != null && servicesSection.Services != null && servicesSection.Services.Count > 0;
      if (isConfig)
        config.LoadFromConfiguration(NomadConfig);
      return isConfig;
    }

    /// <summary>
    /// TODO: Consider what defaults to use here
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
            config.EnableProtocol(new BasicHttpBinding());
            config.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
          }
          else if (uriType == UriType.Tcp)
          {
            config.EnableProtocol(new NetTcpBinding());
          }
          else if (uriType == UriType.NamedPipe)
          {
            config.EnableProtocol(new NetNamedPipeBinding());
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

    #endregion Configuration

    #region OperationContract

    public string ExecuteNomadMethodUsingBinarySerialiser(NomadMethodType methodType, bool runInMainThread, string assemblyFileName, string typeFullName, string methodName, IList<string> serialisedParameters, IList<string> serialisedNonLocalVariables)
    {
      object executionObject = GetExecutionObject(assemblyFileName, typeFullName);
      Assembly asm = executionObject.GetType().Assembly;
      ResolveEventHandler onAssemblyResolve = (s, args) => { return TryResolveAssembly(asm, args.Name); };
      AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;
      try
      {
        //TODO: Experiment with this to see if serialising individual items is better/worse than serialising lists as a whole
        Serialiser serialiser = new Serialiser();
        IList<ParameterVariable> parameters = serialiser.DeserialiseBase64<ParameterVariable>(serialisedParameters, false);
        IList<IVariable> nonLocalVariables = serialiser.DeserialiseBase64<IVariable>(serialisedNonLocalVariables, true);

        NomadMethodResult result = base.ExecuteNomadMethod(methodType, runInMainThread, assemblyFileName, typeFullName, methodName, parameters, nonLocalVariables);
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
        AppDomain.CurrentDomain.AssemblyResolve -= onAssemblyResolve;
      }
    }

    #endregion OperationContract

    #region IDisposable

    public void Dispose()
    {
      if (ServiceHost != null && (ServiceHost.State == CommunicationState.Opening || ServiceHost.State == CommunicationState.Opened))
        Stop();
    }

    #endregion IDisposable
  }
}
