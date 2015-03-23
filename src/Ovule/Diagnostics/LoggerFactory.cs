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
using System.Reflection;

namespace Ovule.Diagnostics
{
  /// <summary>
  /// Will return an instance of whatever logger the application is configured to use.
  /// 
  /// N.B. Currently BufferingTextFileLogger is the only concrete logger implementation.
  /// </summary>
  public class LoggerFactory
  {
    #region Properties/Fields

    private static Type _loggerType;
    public static Type LoggerType
    {
      get { return _loggerType; }
      set
      {
        if (value == null)
          throw new NullReferenceException("Cannot set 'LoggerType' to null");
        if (value.IsInterface || value.IsAbstract || !typeof(ILogger).IsAssignableFrom(value))
          throw new TypeLoadException("'LoggerType' must be a concrete implementation of ILogger");
        _loggerType = value;
      }
    }

    #endregion Properties/Fields

    #region ctors

    static LoggerFactory()
    {
      LoggerType = typeof(BufferingTextFileLogger);
      string loggerTypeConfig = ConfigurationManager.AppSettings["LoggerType"];
      if (!string.IsNullOrWhiteSpace(loggerTypeConfig))
      {
        try
        {
          Type loggerType = Assembly.GetExecutingAssembly().GetType(loggerTypeConfig);
          if (loggerType == null)
            throw new TypeLoadException(string.Format("Cannot find type with name '{0}'", loggerTypeConfig));
        }
        catch (Exception ex)
        {
          throw new TypeLoadException("The application setting 'LoggerType' is invalid.  See inner exception for more details", ex);
        }
      }
    }

    #endregion ctors

    #region Methods

    public static ILogger Create(string logName)
    {
      string logDirectoryString = ConfigurationManager.AppSettings["LoggerDirectory"];
      if (LoggerType == typeof(BlackHoleLogger) || string.IsNullOrWhiteSpace(logDirectoryString))
        return new BlackHoleLogger();

      if (LoggerType == null)
        throw new NullReferenceException("'LoggerType' is null.");

      if (LoggerType == typeof(BufferingTextFileLogger))
        return BufferingTextFileLogger.Create(logName);
      throw new TypeLoadException(string.Format("'{0}' does not know how to create logger of type '{1}'", typeof(LoggerFactory).FullName, LoggerType.FullName));
    }

    #endregion Methods
  }
}
