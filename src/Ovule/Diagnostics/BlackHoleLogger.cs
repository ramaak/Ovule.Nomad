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
  /// <summary>
  /// This type does nothing more than satisfy the ILogger contract.  If logging 
  /// is not required during a process then use this logger and all messages will 
  /// be sent towards the centre of the Milky Way
  /// </summary>
  public class BlackHoleLogger: ILogger
  {
    public void LogInfo(string message)
    {
    }

    public void LogInfo(string message, params object[] formatArgs)
    {
    }

    public void LogWarning(string message)
    {
    }

    public void LogWarning(string message, params object[] formatArgs)
    {
    }

    public void LogError(string message)
    {
    }

    public void LogError(string message, params object[] formatArgs)
    {
    }

    public void LogException(Exception ex, string message)
    {
    }

    public void LogException(Exception ex, string message, params object[] formatArgs)
    {
    }

    public void LogException(Exception ex)
    {
    }
  }
}
