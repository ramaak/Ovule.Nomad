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
using System.Configuration;
using System.IO;
using System.Linq;

namespace Ovule.Diagnostics
{
  /// <summary>
  /// A concrete implementation of BufferingLogger which flushes buffered content to plain text files.
  /// </summary>
  public class BufferingTextFileLogger : BufferingLogger
  {
    #region Properties/Fields

    public const string LogFileExtension = ".txt";

    private static object _fileWriteLock = new object();

    protected string LogDirectory { get; private set; }

    public string LogFilePath { get { return Path.Combine(LogDirectory, LogName + LogFileExtension); } }

    #endregion Properties/Fields

    #region ctors

    public BufferingTextFileLogger(string logDirectory, string logName, LogMessageType minLogLevel, int bufferFlushSeconds = 15)
      : base(logName, minLogLevel, bufferFlushSeconds)
    {
      this.ThrowIfArgumentIsNoValueString(() => logDirectory);

      LogDirectory = logDirectory;
    }

    public BufferingTextFileLogger(string logDirectory, string logName, int bufferFlushSeconds = 15)
      : this(logDirectory, logName, LogMessageType.Info, bufferFlushSeconds)
    {
    }

    #endregion ctors

    #region "Factory"

    public static BufferingTextFileLogger Create(string logName)
    {
      string logDirectoryString = ConfigurationManager.AppSettings["LoggerDirectory"];
      string minLogLevelString = ConfigurationManager.AppSettings["LoggerMinLogLevel"];
      string bufferFlushSecondsString = ConfigurationManager.AppSettings["BufferingLoggerFlushSeconds"];

      LogMessageType minLogLevel = LogMessageType.Warning; //don't want too much logging by default
      int bufferFlushSeconds = 10;

      if (string.IsNullOrWhiteSpace(logDirectoryString))
        throw new ConfigurationErrorsException("The application setting 'LoggerDirectory' has not been specified or is invalid.");

      if (!Directory.Exists(logDirectoryString))
        Directory.CreateDirectory(logDirectoryString);
      //throw new DirectoryNotFoundException(string.Format("The application 'LoggerDirectory' points to a path that doesn't exist.  Please ensure the path is correct and if so create the directory."));

      if (!string.IsNullOrWhiteSpace(minLogLevelString))
      {
        if (!Enum.TryParse<LogMessageType>(minLogLevelString, out minLogLevel))
          throw new ConfigurationErrorsException("The application setting 'LoggerMinLogLevel' is invalid.");
      }

      if (!string.IsNullOrWhiteSpace(bufferFlushSecondsString))
      {
        if (!int.TryParse(bufferFlushSecondsString, out bufferFlushSeconds))
          throw new ConfigurationErrorsException("The application setting 'BufferingLoggerFlushSeconds' is invalid");
      }
      return new BufferingTextFileLogger(logDirectoryString, logName, minLogLevel, bufferFlushSeconds);
    }

    #endregion "Factory"

    #region Overrides

    public override void Flush()
    {
      if (MessageBuffer == null)
        throw new NullReferenceException("MessageBuffer is null");
      if (MessageBuffer.Any())
      {
        string output = "";
        foreach (ILogMessage message in MessageBuffer)
          output += string.Format("{0}:\t[{1}]\t{2}{3}\r\n", message.MessageType.ToString(), message.CreatedAt.ToString(), message.Message,
            string.IsNullOrWhiteSpace(message.AdditionalInformation) ? "" : "\r\n" + message.AdditionalInformation);

        lock (_fileWriteLock)
        {
          if (!Directory.Exists(LogDirectory))
            Directory.CreateDirectory(LogDirectory);
          File.AppendAllText(LogFilePath, output);
        }
      }
    }

    #endregion Overrides
  }
}
