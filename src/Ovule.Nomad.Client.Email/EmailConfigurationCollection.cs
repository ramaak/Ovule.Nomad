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
using System.Collections.Generic;

namespace Ovule.Nomad.Client.Email
{
  public class InboundEmailConfigurationCollection : IConfigurationCollection
  {
    public bool AreAllSettingsRequired { get { return true; } }

    public string InboundEmailHost { get; set; }
    public int InboundEmailPort { get; set; }
    public bool InboundEmailUseSsl { get; set; }
    public string InboundEmailUsername { get; set; }
    public string InboundEmailPassword { get; set; }

    public IList<string> GetValidationErrors()
    {
      //AreAllSettingsRequired == true and don't need anything special
      return null;
    }
  }

  public class OutboundEmailConfigurationCollection : IConfigurationCollection
  {
    public bool AreAllSettingsRequired { get { return true; } }

    public string OutboundEmailHost { get; set; }
    public int OutboundEmailPort { get; set; }
    public bool OutboundEmailUseSsl { get; set; }
    public string OutboundEmailUsername { get; set; }
    public string OutboundEmailPassword { get; set; }
    public string OutboundEmailFromAddress { get; set; }
    public string OutboundEmailToAddress { get; set; }

    public IList<string> GetValidationErrors()
    {
      //AreAllSettingsRequired == true and don't need anything special
      return null;
    }
  }
}
