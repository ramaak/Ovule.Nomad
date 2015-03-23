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
using System.Net;
using System.Net.Mail;

namespace Ovule.Email
{
  /// <summary>
  /// A very basic SMTP client, currently doesn't offer much flexibility in terms of authentication, etc.
  /// </summary>
  public class SmtpEmailSender: IEmailSender
  {
    #region Properties/Fields

    private string _host;
    private string _username;
    private string _password;
    private int _port;
    private bool _useSsl;
    private string _fromAddress;

    #endregion Properties/Fields

    #region ctors

    public SmtpEmailSender(string host, string username, string password, int port, bool useSsl, string fromAddress)
    {
      this.ThrowIfArgumentIsNoValueString(() => host);
      this.ThrowIfArgumentIsNoValueString(() => username);
      this.ThrowIfArgumentIsNoValueString(() => password);
      this.ThrowIfArgumentNotPositive(() => port);
      this.ThrowIfArgumentIsNoValueString(() => fromAddress);

      _host = host;
      _username = username;
      _password = password;
      _port = port;
      _useSsl = useSsl;
      _fromAddress = fromAddress;
    }

    #endregion ctors

    #region IEmailSender

    public void Send(string to, string subject, string body)
    {
      this.ThrowIfArgumentIsNoValueString(() => to);
      this.ThrowIfArgumentIsNoValueString(() => subject);
      this.ThrowIfArgumentIsNoValueString(() => body);

      using (SmtpClient smtp = new SmtpClient(_host, _port))
      {
        smtp.EnableSsl = _useSsl;
        smtp.UseDefaultCredentials = false;
        smtp.Credentials = new NetworkCredential(_username, _password);
        smtp.Send(_fromAddress, to, subject, body);
      };
    }

    #endregion IEmailSender
  }
}
