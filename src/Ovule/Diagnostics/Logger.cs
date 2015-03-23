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
using System.Linq;

namespace Ovule.Diagnostics
{
  /// <summary>
  /// The basis for a logger.  Accepts calls to log messages however message persistence is left to the abstract WriteMessage(ILogMessage) method.
  /// A minimum logging level can be specified which means only message with a severity equal or greater to this minimum level are logged, i.e. 
  /// if MinLogLevel == LogMessageType.Info then all messages are logged, if MinLogLevel == LogMessageType.Warning then only warnings and errors are logged 
  /// and if MinLogLevel == LogMessageType.Error then only errors are logged.
  /// </summary>
  public abstract class Logger: ILogger
  {
    #region Properties/Fields

    public LogMessageType MinLogLevel { get; private set; }
    public string LogName { get; private set; }

    #endregion Properties/Fields

    #region ctors

    public Logger(string logName):this(logName, LogMessageType.Info)
    {
    }

    public Logger(string logName, LogMessageType minLogLevel)
    {
      this.ThrowIfArgumentIsNoValueString(() => logName);
      
      LogName = logName;
      MinLogLevel = minLogLevel;
    }

    #endregion ctors

    #region Abstract

    protected abstract void WriteMessage(ILogMessage message);

    #endregion Abstract

    #region Methods

    private void WriteMessageIfRequired(ILogMessage message)
    {
      bool log = (MinLogLevel & message.MessageType) == MinLogLevel;
      if (log)
        WriteMessage(message);
    }

    private string GetMessage(string message, params object[] formatArgs)
    {
      if (formatArgs != null && formatArgs.Any())
        message = string.Format(message, formatArgs);
      return message;
    }

    #endregion Methods

    #region ILogger

    public virtual void LogInfo(string message, params object[] formatArgs)
    {
      this.ThrowIfArgumentIsNoValueString(() => message);

      WriteMessageIfRequired(new InfoLogMessage(GetMessage(message, formatArgs)));
    }

    public virtual void LogInfo(string message)
    {
      LogInfo(message, null);
    }

    public virtual void LogWarning(string message, params object[] formatArgs)
    {
      this.ThrowIfArgumentIsNoValueString(() => message);

      WriteMessageIfRequired(new WarningLogMessage(GetMessage(message, formatArgs)));
    }

    public virtual void LogWarning(string message)
    {
      LogWarning(message, null);
    }

    public virtual void LogError(string message, params object[] formatArgs)
    {
      this.ThrowIfArgumentIsNoValueString(() => message);

      WriteMessageIfRequired(new ErrorLogMessage(GetMessage(message, formatArgs)));
    }

    public virtual void LogError(string message)
    {
      LogError(message, null);
    }

    public virtual void LogException(Exception exception)
    {
      this.ThrowIfArgumentIsNull(() => exception);

      WriteMessageIfRequired(new ExceptionLogMessage(exception));
    }

    public virtual void LogException(Exception exception, string message, params object[] formatArgs)
    {
      this.ThrowIfArgumentIsNoValueString(() => message);
      this.ThrowIfArgumentIsNull(() => exception);

      WriteMessageIfRequired(new ExceptionLogMessage(GetMessage(message, formatArgs), exception));
    }

    public virtual void LogException(Exception exception, string message)
    {
      LogException(exception, message, null);
    }

    #endregion ILogger
  }
}
