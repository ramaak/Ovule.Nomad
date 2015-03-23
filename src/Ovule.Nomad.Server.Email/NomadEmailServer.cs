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
using Ovule.Email;
using System;
using System.Collections.Generic;

namespace Ovule.Nomad.Server.Email
{
  /// <summary>
  /// A concrete implementation of NomadServer that uses email as the communications mechanism.
  /// 
  /// This implementation was made more for fun and to demonstrate how NomadServers can be be developed very 
  /// easily.  However it could well have practical uses where firewalls or NAT are a major issues or in places
  /// where something like MSMQ would be considered. Using email as the transport mechanism is obviously very
  /// slow though.
  /// </summary>
  public class NomadEmailServer : NomadServer, IDisposable
  {
    #region Properties/Fields

    private const string ExecuteNomadicMethodRequestEmailSubject = "ExecuteNomadicMethod-Request";
    private const string ExecuteNomadicMethodResponseEmailSubject = "ExecuteNomadicMethod-Response";
    private const char EmailPartDelimiter = '>';

    private static ILogger _logger = LoggerFactory.Create(typeof(NomadEmailServer).FullName);

    private IEmailMonitor _emailMonitor;
    private IEmailSender _emailSender;
    private bool _isStarted;

    #endregion Properties/Fields

    #region ctors

    /// <summary>
    /// 
    /// </summary>
    /// <param name="emailMonitor"></param>
    /// <param name="emailSender"></param>
    public NomadEmailServer(IEmailMonitor emailMonitor, IEmailSender emailSender)
    {
      this.ThrowIfArgumentIsNull(() => emailMonitor);
      this.ThrowIfArgumentIsNull(() => emailSender);

      _emailMonitor = emailMonitor;
      _emailSender = emailSender;

      _emailMonitor.EmailReceived += OnEmailReceived;
    }

    #endregion ctors

    #region Event Handling

    /// <summary>
    /// This will fire whenever the IEmailMonitor receives an email.  It checks if the email is a request to execute a momadic method 
    /// and if so executes it.  After execution a email is sent to the sender of the original email with the results the method execution
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnEmailReceived(object sender, EmailReceivedEventArgs e)
    {
      try
      {
        if (e != null && e.Subject != null && e.Subject.StartsWith(ExecuteNomadicMethodRequestEmailSubject))
        {
          _logger.LogInfo("OnEmailReceived: Received email with subject '{0}' from sender '{1}'", e.Subject, e.From);
          if (string.IsNullOrWhiteSpace(e.From))
            throw new ArgumentException(string.Format("Received email with subject '{0}' however the sender (to respond to) could not be determined", e.Subject));

          if (string.IsNullOrWhiteSpace(e.Body))
            throw new ArgumentException(string.Format("Received email with subject '{0}' however there was no content", e.Subject));

          int requestGuidIndex = e.Subject.IndexOf(EmailPartDelimiter);
          if (requestGuidIndex < 0)
            throw new IndexOutOfRangeException(string.Format("Received email with subject '{0}' however the request GUID could not be determined", e.Subject));

          Guid requestGuid = Guid.Empty;
          string requestGuidString = e.Subject.Substring(requestGuidIndex + 1);
          if (!Guid.TryParse(requestGuidString, out requestGuid))
            throw new InvalidOperationException(string.Format("Received email with subject '{0}' however the request GUID could not be determined", e.Subject));

          string[] parts = e.Body.Split(EmailPartDelimiter);
          if (parts == null || parts.Length != 7)
            throw new InvalidOperationException(string.Format("OnEmailReceived: Recevied email with invalid content expected there to be 5 components but there were '{0}'.  Content is:\r\n{1}", parts.Length, e.Body));

          NomadMethodType methodType = NomadMethodType.Normal;
          if (!Enum.TryParse<NomadMethodType>(parts[0], true, out methodType))
            throw new InvalidOperationException(string.Format("Expected to read value of type '{0}' but read '{1}'", typeof(NomadMethodType).FullName, parts[0]));

          bool runInMainThread = false;
          if (!bool.TryParse(parts[1], out runInMainThread))
            throw new InvalidOperationException(string.Format("Expected to read boolean value but read '{0}'", parts[1]));

          string assemblyFilename = parts[2];
          string typeFullName = parts[3];
          string methodName = parts[4];
          string serialisedParameters = parts[5];
          string serialisedNonLocalVariables = parts[6];

          Serialiser serialiser = new Serialiser();
          IList<ParameterVariable> parameters = null;
          IList<IVariable> nonLocalVariables = null;
          if (!string.IsNullOrWhiteSpace(serialisedParameters))
            parameters = serialiser.DeserialiseBase64<IList<ParameterVariable>>(serialisedParameters);
          if (!string.IsNullOrWhiteSpace(serialisedNonLocalVariables))
            nonLocalVariables = serialiser.DeserialiseBase64<IList<IVariable>>(serialisedNonLocalVariables);

          NomadMethodResult result = base.ExecuteNomadMethod(methodType, runInMainThread, assemblyFilename, typeFullName, methodName, parameters, nonLocalVariables);

          string serialisedResult = serialiser.SerialiseToBase64(result);

          string replySubject = string.Format("{0}{1}{2}", ExecuteNomadicMethodResponseEmailSubject, EmailPartDelimiter, requestGuid);
          _emailSender.Send(e.From, replySubject, serialisedResult);

          _logger.LogInfo("OnEmailReceived: Sent email response to '{0}' for request '{1}'", e.From, requestGuid);

          e.IsMessageDeleteRequested = true;

          _logger.LogInfo("OnEmailReceived: Complete");
        }
      }
      catch (Exception ex)
      {
        _logger.LogException(ex);
      }
    }

    #endregion Event Handling

    #region Methods

    /// <summary>
    /// Start the service.  Before this is called emails requesting execution of nomadic methods will be ignored.
    /// </summary>
    public void Start()
    {
      if (!_isStarted)
      {
        _emailMonitor.Start();
        _isStarted = true;
        _logger.LogInfo("Start: {0} started", this.GetType().FullName);
      }
    }

    /// <summary>
    /// Stop the service.  After this is called emails requesting execution of nomadic methods will be ignored.
    /// </summary>
    public void Stop()
    {
      if (_isStarted)
      {
        _emailMonitor.Stop();
        _isStarted = false;
        _logger.LogInfo("Stop: {0} stopped", this.GetType().FullName);
      }
    }

    #endregion Methods

    #region IDisposable

    public void Dispose()
    {
      _emailMonitor.Dispose();
      _logger.LogInfo("Dispose: {0} disposed", this.GetType().FullName);
    }

    #endregion IDisposable
  }
}
