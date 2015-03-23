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
using AE.Net.Mail;
using AE.Net.Mail.Imap;
using Ovule.Diagnostics;
using System;

namespace Ovule.Email
{
  /// <summary>
  /// A basic IMAP client which listens for incomming email and fires an EmailReceived event whenever one arrives.
  /// </summary>
  public class ImapEmailMonitor: IEmailMonitor
  {
    #region Events

    public event EventHandler<EmailReceivedEventArgs> EmailReceived;

    #endregion Events

    #region Properties/Fields

    public static ILogger _logger = LoggerFactory.Create(typeof(ImapEmailMonitor).FullName);

    private string _host;
    private string _username;
    private string _password;
    private int _port;
    private bool _useSsl;
    private ImapClient _client;

    public bool IsStarted { get; private set; }

    #endregion Properties/Fields

    #region ctors

    public ImapEmailMonitor(string host, string username, string password, int port, bool useSsl)
    {
      this.ThrowIfArgumentIsNoValueString(() => host);
      this.ThrowIfArgumentIsNoValueString(() => username);
      this.ThrowIfArgumentIsNoValueString(() => password);
      this.ThrowIfArgumentNotPositive(() => port);

      _host = host;
      _username = username;
      _password = password;
      _port = port;
      _useSsl = useSsl;

      _client = new ImapClient();
      _client.AuthMethod = AuthMethods.Login;
    }

    #endregion ctors

    #region Event Handling

    private void OnNewMessage(object sender, MessageEventArgs e)
    {
      MailMessage msg = _client.GetMessage(e.MessageCount - 1);
      if (msg == null)
        _logger.LogError("Received an email however it could not be retrieved from server");
      else
      {
        _logger.LogInfo("Received email from '{0}' with subject '{1}'", msg.From.Address, msg.Subject);

        EmailReceivedEventArgs args = new EmailReceivedEventArgs(msg.From.Address, msg.Subject, msg.Body.Trim());
        if (EmailReceived != null)
        {
          EmailReceived(this, args);
          if (args.IsMessageDeleteRequested)
          {
            try
            {
              _client.DeleteMessage(msg);
            }
            catch (Exception ex)
            {
              _logger.LogException(ex, "Email deletion was requested however the email could not be deleted");
            }
          }
        }
      }
    }

    #endregion Event Handling

    #region IEmailClient

    public void Start()
    {
      if (IsStarted)
        return;
      try
      {
        _client.Connect(_host, _port, _useSsl, true);
        _client.Login(_username, _password);
        _client.NewMessage += OnNewMessage;
        
        IsStarted = true;

        _logger.LogInfo("Start: Monitor started");
      }
      catch(Exception ex)
      {
        _logger.LogException(ex, "Start: Error");
      }
    }

    public void Stop()
    {
      if (!IsStarted)
        return;
      try
      {
        IsStarted = false;

        _client.NewMessage -= OnNewMessage;
        _client.Logout();
        _client.Disconnect();

        _logger.LogInfo("Stop: Monitor stopped");
      }
      catch (Exception ex)
      {
        _logger.LogException(ex, "Stop: Error");
      }
    }

    public void Send(string to, string subject, string body)
    {
      _logger.LogInfo("Send: To '{0}', subject '{1}', body:\r\n'{2}'", to, subject, body);

      if (!_client.IsConnected && !_client.IsAuthenticated)
        throw new ImapClientException(string.Format("'{0}' is not in a state in which it can send an email.  Try calling Start().  IsConnected = '{1}', IsAuthenticated = '{2}'",
          this.GetType().FullName, _client.IsConnected, _client.IsAuthenticated));
    }

    #endregion IEmailClient

    #region IDisposable

    public void Dispose()
    {
      if (_client != null && !_client.IsDisposed)
      {
        if (_client.IsConnected)
          Stop();
        _client.Dispose();
        _client = null;
      }
    }

    #endregion IDisposable
  }
}
