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
using Ovule.Nomad.Server.Email;
using System;
using System.Configuration;
using System.IO;

namespace Ovule.Nomad.Server.Stock
{
  public class StockNomadServer
  {
    public const string NomadServerUriConfig = "NomadServerUri";

    private static ILogger _logger = LoggerFactory.Create(typeof(StockNomadServer).FullName);

    public static void Main(string[] args)
    {
      try
      {
        string configFileDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("{0}.dll", typeof(INomadServer).Assembly.GetName().Name));
        System.Configuration.Configuration nomadConfig = ConfigurationManager.OpenExeConfiguration(configFileDll);
        KeyValueConfigurationElement serverUriSetting = nomadConfig.AppSettings.Settings[NomadServerUriConfig];
        if(serverUriSetting == null || string.IsNullOrWhiteSpace(serverUriSetting.Value))
          throw new NomadServerInitialisationException("The '{0}' configuration setting is missing or invalid", NomadServerUriConfig);

        string serverUriString = nomadConfig.AppSettings.Settings[NomadServerUriConfig].Value;
        Uri serverEndpointUri = new Uri(serverUriString);
        
        Console.WriteLine("\r\n**** THIS IS A TEST NOMAD SERVER, DO NOT USE IT FOR ANYTHING OTHER THAN TESTING ****\r\n");

        UriType uriType = UriUtils.GetType(serverEndpointUri);
        if (uriType == UriType.Email)
          StartEmailServer(serverEndpointUri, nomadConfig);
        else
          StartWcfServer(serverEndpointUri);
      }
      catch (Exception ex)
      {
        _logger.LogException(ex, "The problem may be security related.  Please ensure the process is run under an account with adequate privileges");
        Console.WriteLine("A fatal error has occurred and the server will now shutdown.\r\n" +
          "The problem may be security related.  Please ensure the process is run under an account with adequate privileges.\r\n" +
          "{0}", ex.Message);
      }
    }

    private static void StartWcfServer(Uri endpointUri)
    {
      NomadWcfServer server = new NomadWcfServer(endpointUri);
      server.Start();

      Console.WriteLine("Started '{0}' which is listening at '{1}'...", server.GetType().Name, endpointUri);
      Console.WriteLine("Hit enter to shutdown");
      Console.ReadLine();

      server.Stop();
    }

    private static void StartEmailServer(Uri endpointUri, System.Configuration.Configuration config)
    {
      Console.WriteLine("Starting Nomad Email Server...");

      InboundEmailConfigurationCollection inEmailConfig = new AppSettingsConfigurationInitialiser<InboundEmailConfigurationCollection>().Initialise(config);
      OutboundEmailConfigurationCollection outEmailConfig = new AppSettingsConfigurationInitialiser<OutboundEmailConfigurationCollection>().Initialise(config);

      ImapEmailMonitor emailMonitor = new ImapEmailMonitor(
        inEmailConfig.InboundEmailHost,
        inEmailConfig.InboundEmailUsername,
        inEmailConfig.InboundEmailPassword,
        inEmailConfig.InboundEmailPort,
        inEmailConfig.InboundEmailUseSsl
        );

      SmtpEmailSender emailSender = new SmtpEmailSender(
        outEmailConfig.OutboundEmailHost,
        outEmailConfig.OutboundEmailUsername,
        outEmailConfig.OutboundEmailPassword,
        outEmailConfig.OutboundEmailPort,
        outEmailConfig.OutboundEmailUseSsl,
        outEmailConfig.OutboundEmailFromAddress
        );

      using (NomadEmailServer server = new NomadEmailServer(emailMonitor, emailSender))
      {
        server.Start();

        Console.WriteLine("Hit enter to shutdown");
        Console.ReadLine();
      }
    }
  }
}
