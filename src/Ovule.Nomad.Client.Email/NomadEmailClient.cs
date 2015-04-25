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
using Ovule.Configuration;
using Ovule.Diagnostics;
using Ovule.Email;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ovule.Nomad.Client.Email
{
  /// <summary>
  /// 
  /// ************ TODO: A lot has changed since this was last tested and it likely has issues now ************
  /// 
  /// A concrete implementation of NomadClient that uses email as the communications mechanism.
  /// 
  /// This implementation was made more for fun and to demonstrate how unusual NomadClients can be be developed
  /// however it could well have practical uses where firewalls or NAT are a major issues or in places
  /// where something like MSMQ would be considered.  Using email as the transport mechanism is obviously very
  /// slow though.
  /// </summary>
  public class NomadEmailClient : NomadClient
  {
    #region Properties/Fields

    private const string ExecuteNomadicMethodRequestEmailSubject = "ExecuteNomadicMethod-Request";
    private const string ExecuteNomadicMethodResponseEmailSubject = "ExecuteNomadicMethod-Response";
    private const char EmailPartDelimiter = '>';

    private static ILogger _logger = LoggerFactory.Create(typeof(NomadEmailClient).FullName);

    private static string _serverEmailAddress;
    private static IEmailMonitor _emailMonitor;
    private static IEmailSender _emailSender;

    private bool _isEmailResponseReceived;

    #endregion Properties/Fields

    #region ctors

    /// <summary>
    /// 
    /// </summary>
    static NomadEmailClient()
    {

      string configFileDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("{0}.dll", typeof(INomadClient).Assembly.GetName().Name));
      NomadConfig = ConfigurationManager.OpenExeConfiguration(configFileDll);

      InboundEmailConfigurationCollection inEmailConfig = new AppSettingsConfigurationInitialiser<InboundEmailConfigurationCollection>().Initialise(NomadConfig);
      OutboundEmailConfigurationCollection outEmailConfig = new AppSettingsConfigurationInitialiser<OutboundEmailConfigurationCollection>().Initialise(NomadConfig);

      _emailMonitor = new ImapEmailMonitor(
        inEmailConfig.InboundEmailHost,
        inEmailConfig.InboundEmailUsername,
        inEmailConfig.InboundEmailPassword,
        inEmailConfig.InboundEmailPort,
        inEmailConfig.InboundEmailUseSsl
        );

      _emailSender = new SmtpEmailSender(
        outEmailConfig.OutboundEmailHost,
        outEmailConfig.OutboundEmailUsername,
        outEmailConfig.OutboundEmailPassword,
        outEmailConfig.OutboundEmailPort,
        outEmailConfig.OutboundEmailUseSsl,
        outEmailConfig.OutboundEmailFromAddress
        );

      _serverEmailAddress = outEmailConfig.OutboundEmailToAddress;
    }

    #endregion ctors

    #region NomadClient

    /// <summary>
    /// Sends an execute request to a Nomad email service
    /// 
    /// N.B. This class is a bit behind the curve and 'endpoint' is currently ignored so only default server will receive requests
    /// </summary>
    /// <param name="assemblyName">The name of the assembly (without directory path) that contains the type to execute, e.g. "MyFancyAssembly.dll"</param>
    /// <param name="typeFullName">The name of the type that contains the method to execute, e.g. "MyFancyApplication.MyFancyType"</param>
    /// <param name="methodName">The name of the method with 'typeFullName' to execute on the server</param>
    /// <param name="parameters">The parameters to pass to method 'methodName', e.g. "MyFancyMethod"</param>
    /// <param name="nonLocalVariables">A collection of fields/properties that are currently with reach of method 'methodName', methods it calls, methods they call, etc.</param>
    /// <returns></returns>
    protected override NomadMethodResult IssueServerRequest(Uri endpoint, NomadMethodType methodType, string assemblyName, string assemblyFileHash, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables)
    {
      _logger.LogInfo("IssueServerRequest: For assembly '{0}', type '{1}' and method '{2}", assemblyName, typeFullName, methodName);

      string serialisedParameters = null;
      string serialisedNonLocalVariables = null;
      Serialiser serialiser = new Serialiser();
      if (parameters != null && parameters.Any())
        serialisedParameters = serialiser.SerialiseToBase64((object)parameters);
      if (nonLocalVariables != null && nonLocalVariables.Any())
        serialisedNonLocalVariables = serialiser.SerialiseToBase64((object)nonLocalVariables);

      string requestEmailBody = string.Format("{0}>{1}>{2}>{3}>{4}>{5}>{6}", methodType, assemblyName, assemblyFileHash, typeFullName, methodName, serialisedParameters, serialisedNonLocalVariables);
      NomadMethodResult result = SendServerRequestEmailAndWaitForResponse(requestEmailBody);

      _logger.LogInfo("IssueServerRequest: Complete");

      return result;
    }

    protected override NomadMethodResult IssueServerRequest(Uri endpoint, NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables)
    {
      throw new NotImplementedException(string.Format("This form of request has not been implemented on the '{0}'", typeof(NomadEmailClient).FullName));
    }

    #endregion NomadClient

    #region Email

    /// <summary>
    /// Sends email to server with execute request, waits for server reply (via email) and returns result.
    /// </summary>
    /// <param name="requestEmailBody">Details of the method to execute and the execution context</param>
    /// <returns></returns>
    private NomadMethodResult SendServerRequestEmailAndWaitForResponse(string requestEmailBody)
    {
      //will send the request with this Guid and the response from the server will include the same Guid.
      //this means we won't accidentally accept the results of some other request - if emails are delivered 
      //out of order, or there are threads, etc.
      Guid requestGuid = Guid.NewGuid();
      string emailSubject = string.Format("{0}{1}{2}", ExecuteNomadicMethodRequestEmailSubject, EmailPartDelimiter, requestGuid);

      _isEmailResponseReceived = false;
      NomadMethodResult result = null;
      EventHandler<EmailReceivedEventArgs> onEmailReceived = (o, e) => { ProcessReceivedEmail(requestGuid, e, out result); };
      _emailMonitor.EmailReceived += onEmailReceived;
      _emailMonitor.Start();

      _emailSender.Send(_serverEmailAddress, emailSubject, requestEmailBody);

      _logger.LogInfo("SendServerRequestEmailAndWaitForResponse: Request '{0}' sent to server, waiting for response", requestGuid);

      WaitForResponse(NomadServerResponseTimeout);

      _emailMonitor.EmailReceived -= onEmailReceived;
      //could stop the _emailMonitor just now but probably best to stay logged in so future calls are quicker
      //_emailMonitor.Stop();

      if (result == null)
        throw new NomadException(string.Format("Did not receive email response from server within the configured timeframe of '{0}'", NomadServerResponseTimeout));

      _logger.LogInfo("SendServerRequestEmailAndWaitForResponse: Response recevied for request '{0}'", requestGuid);
      return result;
    }

    /// <summary>
    /// Method blocks the caller until either the server responds or 'timeout' elapses.  If 'timeout' is null then it will block forever if the
    /// server doesn't respond.
    /// </summary>
    private void WaitForResponse(TimeSpan timeout)
    {
      int sleepMillis = 500; //TOOD: have default which can be overriden with config
      int totalSleepTime = 0;
      bool canTimeout = timeout > TimeSpan.Zero;
      /// N.B. Would have been preferrable to use a reset event or something to this implementation (which is just a spinlock) however need to keep the 
      /// thread alive so email received events fire.  Consider changing implementation but this class isn't high priority.
      while (!_isEmailResponseReceived)
      {
        Thread.Sleep(sleepMillis);
        totalSleepTime += sleepMillis;
        if (canTimeout && totalSleepTime > timeout.TotalMilliseconds)
          throw new TimeoutException(string.Format("Did not receive a response from the server within the alloted timeframe of '{0}'", timeout));
      }
    }

    /// <summary>
    /// This methods called whenever an email arrives between the time an execution request is sent and the response is received.
    /// </summary>
    /// <param name="requestGuid">The id of the request that we're waiting on a response for</param>
    /// <param name="e">Details of the email that has been received</param>
    /// <param name="result">If the email is the response being waited on then result is set from the contents of the email</param>
    private void ProcessReceivedEmail(Guid requestGuid, EmailReceivedEventArgs e, out NomadMethodResult result)
    {
      try
      {
        result = null;
        _logger.LogInfo("ProcessReceivedEmail: Received email with subject '{0}'", e.Subject);
        if (e != null && !string.IsNullOrWhiteSpace(e.Subject) && e.Subject.StartsWith(ExecuteNomadicMethodResponseEmailSubject))
        {
          int guidIndex = e.Subject.IndexOf(EmailPartDelimiter);
          if (guidIndex > -1)
          {
            string responseGuidString = e.Subject.Substring(guidIndex + 1);
            Guid responseGuid = Guid.Empty;
            if (!Guid.TryParse(responseGuidString, out responseGuid))
              throw new NomadException(string.Format("Received email from server with subject '{0}' but could not parse the response GUID", e.Subject));

            if (responseGuid == requestGuid)
            {
              if (string.IsNullOrWhiteSpace(e.Body))
                throw new NomadException(string.Format("Received email from server with subject '{0}' but it contained no content", e.Subject));

              result = new Serialiser().DeserialiseBase64<NomadMethodResult>(e.Body);

              //set SendServerRequestEmailAndWaitForResponse free
              _isEmailResponseReceived = true;
            }
          }
        }
      }
      catch (Exception ex)
      {
        //don't leave the SendServerRequestEmailAndWaitForResponse hanging
        _isEmailResponseReceived = true;
        _logger.LogException(ex, "ProcessReceivedEmail: Error");
        throw;
      }
    }

    #endregion Email
  }
}
