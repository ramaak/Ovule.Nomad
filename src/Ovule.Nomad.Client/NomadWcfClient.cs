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
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Threading;

namespace Ovule.Nomad.Client
{
  /// <summary>
  /// A base implementation of NomadClient that uses WCF as the communications mechanism.
  /// NomadClient itself does not know nor care about the transport mechanism and can be used with any form of network comms. 
  /// 
  /// N.B. The setting 'NomadServerResponseTimeout' that is known to NomadClient is not used here.  Instead standard WCF configuration should be used.
  /// TODO: Sort this out, want things to be consistent across all server types and also keep configuration requirements to a minimum.
  /// 
  /// </summary>
  public class NomadWcfClient : NomadClient
  {
    #region Properties/Fields

    public const string NomadServerUriConfig = "NomadServerUri";

    private static ILogger _logger = LoggerFactory.Create(typeof(NomadWcfClient).FullName);
    private static object _getClientFactoryLock = new object();
    private static bool _haveWarnedAboutBinarySerialiser;

    public static Uri DefaultNomadServerUri { get; private set; }

    /// <summary>
    /// Cache of each ChannelFactory that's created.  I beleive .Net should be doing this however this does give measurable 
    /// performance improvement.
    /// </summary>
    protected static IDictionary<string, ChannelFactory<INomadWcfService>> ChannelFactories { get; private set; }

    #endregion Properties/Fields

    #region ctors

    static NomadWcfClient()
    {
      try
      {
        KeyValueConfigurationElement serverUriSetting = NomadConfig.AppSettings.Settings[NomadServerUriConfig];
        if(serverUriSetting == null || string.IsNullOrWhiteSpace(serverUriSetting.Value))
          throw new NomadClientInitialisationException("Application configuration value for '{0}' is missing", NomadServerUriConfig);

        string defaultServerUriString = NomadConfig.AppSettings.Settings[NomadServerUriConfig].Value;
        try
        {
          DefaultNomadServerUri = new Uri(defaultServerUriString);
        }
        catch (Exception pex)
        {
          throw new NomadClientInitialisationException(pex, "Application configuration value for '{0}' is invalid", NomadServerUriConfig);
        }
      }
      catch(Exception ex)
      {
        _logger.LogException(ex);
        throw;
      }
    }

    public NomadWcfClient()
      : base()
    {
      try
      {
        if (ChannelFactories == null)
        {
          PrimeDefaultServerChannel();
          ChannelFactories = new Dictionary<string, ChannelFactory<INomadWcfService>>();
        }
      }
      catch(Exception ex)
      {
        _logger.LogException(ex);
        throw;
      }
    }

    #endregion ctors

    #region Util

    protected bool IsCustomConfiguration()
    {
      //TODO: move to using nomad specific config file
      ClientSection clientSection = ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;
      bool isConfig = clientSection != null && clientSection.Endpoints != null && clientSection.Endpoints.Count > 0;
      return isConfig;
    }

    #endregion Util

    #region NomadClient

    /// <summary>
    /// Sends an execute request to a WCF Nomad service
    /// </summary>
    /// <param name="assemblyName">The name of the assembly (without directory path) that contains the type to execute, e.g. "MyFancyAssembly.dll"</param>
    /// <param name="typeFullName">The name of the type that contains the method to execute, e.g. "MyFancyApplication.MyFancyType"</param>
    /// <param name="methodName">The name of the method with 'typeFullName' to execute on the server</param>
    /// <param name="parameters">The parameters to pass to method 'methodName', e.g. "MyFancyMethod"</param>
    /// <param name="nonLocalVariables">A collection of fields/properties that are currently with reach of method 'methodName', methods it calls, methods they call, etc.</param>
    /// <returns></returns>
    protected override NomadMethodResult IssueServerRequest(Uri endpoint, NomadMethodType methodType, bool runInMainThread, string assemblyName, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables)
    {
      INomadWcfService channel = null;
      try
      {
        _logger.LogInfo("IssueServerRequest: For request type '{0}', run in main thread '{1}', assembly '{2}', type '{3}' and method '{4}",
          methodType.ToString(), runInMainThread, assemblyName, typeFullName, methodName);

        if (endpoint == null)
          endpoint = DefaultNomadServerUri;

        channel = GetChannel(endpoint);

        NomadMethodResult result = null;

        if (KnownTypeLocator.KnownTypes == null || !KnownTypeLocator.KnownTypes.Any())
        {
          if (!_haveWarnedAboutBinarySerialiser)
          {
            _logger.LogWarning("IssueServerRequest: No KnownTypes found so cannot use DataContractSerialiser. Falling back to the BinaryFormatter which may have a negative impact on application performance.");
            _haveWarnedAboutBinarySerialiser = true;
          }
          Serialiser serialiser = new Serialiser();
          IList<string> serialisedParameters = serialiser.SerialiseToBase64(parameters);
          IList<string> serialisedNonLocalVariables = serialiser.SerialiseToBase64(nonLocalVariables);
          string serialisedResult = channel.ExecuteNomadMethodUsingBinarySerialiser(methodType, runInMainThread, assemblyName, typeFullName, methodName, serialisedParameters, serialisedNonLocalVariables);
          if (serialisedResult != null)
            result = serialiser.DeserialiseBase64<NomadMethodResult>(serialisedResult);
        }
        else
          result = channel.ExecuteNomadMethod(methodType, runInMainThread, assemblyName, typeFullName, methodName, parameters, nonLocalVariables);

        if (result == null)
          throw new NomadException(string.Format("Did not receive a response from the Nomad service for method '{0}.{1}'", typeFullName, methodName));

        _logger.LogInfo("IssueServerRequest: Complete");

        return result;
      }
      catch (Exception ex)
      {
        if (channel != null)
        {
          ((ICommunicationObject)channel).Abort();
          channel = null;
        }
        throw new NomadException(string.Format("Exception executing nomad method, see inner exception for more details: '{0}'", ex.Message), ex);
      }
      finally
      {
        if (channel != null)
        {
          ((ICommunicationObject)channel).Close();
          channel = null;
        }
      }
    }

    #endregion NomadClient

    #region Service Comms

    protected ChannelFactory<INomadWcfService> ConstructChannelFactoryWithDefaultSettings(Uri uri)
    {
      this.ThrowIfArgumentIsNull(() => uri);

      _logger.LogInfo("ConstructDefaultChannelFactory: Creating default ChannelFactory");

      Binding binding = null;
      EndpointAddress address = new EndpointAddress(uri);
      UriType uriType = UriUtils.GetType(uri);
      if(uriType == UriType.Http)
      {
        binding = new BasicHttpBinding();
      }
      else if(uriType == UriType.Tcp)
      {
        binding = new NetTcpBinding();
      }
      else if(uriType == UriType.NamedPipe)
      {
        binding = new NetNamedPipeBinding();
      }

      if (binding == null)
        throw new NomadClientException(string.Format("Unexpected endpoint type of '{0}'", uriType.ToString()));

      ChannelFactory<INomadWcfService> factory = new ChannelFactory<INomadWcfService>(binding, address);
      return factory;
    }

    /// <summary>
    /// It can take a while to create a channel for the first time (depending on the number of known types).
    /// create the first channel now, allowing other stuff to go on at the same time.  There's a chance that
    /// by the time application code get's to the point of issuing a request the channel's already created and 
    /// cached.  If not then the calling application will block in GetChannel() but it won't have to wait as long
    /// as it would have.
    /// </summary>
    protected virtual void PrimeDefaultServerChannel()
    {
      ThreadPool.QueueUserWorkItem((tCtxt) =>
      {
        GetChannel(DefaultNomadServerUri);
        _logger.LogInfo("Server channel of type '{0}' primed", this.GetType().FullName);
      });
    }

    /// <summary>
    /// Returns a channel to communicate to the WCF service over
    /// </summary>
    /// <returns></returns>
    protected virtual INomadWcfService GetChannel(Uri uri)
    {
      this.ThrowIfArgumentIsNull(() => uri);

      string endpointUriString = uri.ToString();
      if (!ChannelFactories.ContainsKey(endpointUriString))
      {
        lock (_getClientFactoryLock)
        {
          if (!ChannelFactories.ContainsKey(endpointUriString))
          {
            ChannelFactory<INomadWcfService> factory = null;
            if (IsCustomConfiguration()) 
              factory = new ChannelFactory<INomadWcfService>(); //keeping simple for now but will have to cater for multiple endpoints being configured
            else
              factory = ConstructChannelFactoryWithDefaultSettings(uri);

            _logger.LogInfo("GetChannel: Creating ChannelFactory for endpoint '{0}'", factory.Endpoint.Address.Uri);

            ChannelFactories.Add(endpointUriString, factory);
          }
        }
      }
      INomadWcfService channel = ChannelFactories[endpointUriString].CreateChannel();
      return channel;
    }

    #endregion Service Comms
  }
}
