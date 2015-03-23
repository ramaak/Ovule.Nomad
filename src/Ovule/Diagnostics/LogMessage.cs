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

namespace Ovule.Diagnostics
{
  public class LogMessage : ILogMessage
  {
    public DateTime CreatedAt { get; set; }
    public LogMessageType MessageType { get; private set; }
    public string Message { get; private set; }
    public string AdditionalInformation { get; private set; }

    public LogMessage(LogMessageType messageType, string message)
      : this(messageType, message, null)
    {
    }

    public LogMessage(LogMessageType messageType, string message, string additionalInformation)
    {
      this.ThrowIfArgumentIsNoValueString(() => message);

      CreatedAt = DateTime.Now;
      MessageType = messageType;
      Message = message;
      AdditionalInformation = additionalInformation;
    }
  }
}
