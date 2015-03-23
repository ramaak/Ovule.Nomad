using Ovule.Nomad.Client;
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
using System;

namespace Ovule.Nomad.Processor.Gui
{
  public enum NetworkCommunicationsType { Http = 8080, Tcp = 8557, NamedPipes, Email }

  public class NetworkCommunicationsNomadClientMapping
  {
    public NetworkCommunicationsType NetworkCommsType { get; private set; }
    public string NetworkCommsTypeDescription { get { return GetNetworkCommsTypeDescription(); } }
    public Type NomadClientType { get; private set; }

    public NetworkCommunicationsNomadClientMapping(NetworkCommunicationsType commsType, Type nomadClientType)
    {
      if (nomadClientType == null || !(typeof(INomadClient).IsAssignableFrom(nomadClientType)) || nomadClientType.IsAbstract || nomadClientType.IsInterface)
        throw new ArgumentException(string.Format("The 'nomadClientType' argument is invalid.  It must be a concrete class that implements '{0}'", typeof(INomadClient).FullName));

      NetworkCommsType = commsType;
      NomadClientType = nomadClientType;
    }

    private string GetNetworkCommsTypeDescription()
    {
      switch(NetworkCommsType)
      {
        case NetworkCommunicationsType.Http:
          return "HTTP";
        case NetworkCommunicationsType.Tcp:
          return "TCP";
        case NetworkCommunicationsType.NamedPipes:
          return "Named Pipes";
        case NetworkCommunicationsType.Email:
          return "Email";
        default:
          return "UNKNOWN";
      }
    }
  }
}
