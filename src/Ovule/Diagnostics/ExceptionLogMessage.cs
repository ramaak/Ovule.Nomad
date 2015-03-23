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
  public class ExceptionLogMessage : LogMessage, IExceptionLogMessage
  {
    #region Properties/Fields

    public Exception Exception { get; private set; }

    #endregion Properties/Fields

    #region ctors

    public ExceptionLogMessage(Exception exception) : this("An error has occurred", exception) { }

    public ExceptionLogMessage(string message, Exception exception)
      : base(LogMessageType.Error, message, GetExceptionDetails(exception))
    {
      this.ThrowIfArgumentIsNull(() => exception);

      Exception = exception;
    }

    #endregion ctors

    #region Methods

    private static string GetExceptionDetails(Exception exception)
    {
      if (exception == null)
        throw new ArgumentNullException("'exception' is null");
      Exception workingException = exception;
      string details = "";
      int exceptionLevel = 0;
      while(workingException != null)
      {
        details += string.Format(
@"  ******* Start Exception - Level[{0}] *******
  {1}
  {2}
  ******* End Exception - Level[{0}] *******
", 
          exceptionLevel, workingException.Message, workingException.StackTrace);
        workingException = workingException.InnerException;
        exceptionLevel++;
      }
      return details;
    }

    #endregion Methods
  }
}
