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
using System.Collections.Generic;
using System.Timers;

namespace Ovule.Diagnostics
{
  /// <summary>
  /// Writes messages to a buffer and flushes it every so often.  
  /// If an exception message is logged the buffer is flushed immediatly as otherwise the application may terminate before the next flush.
  /// This also types listens to UnhandledException and ProcessExit events and flushes the buffer when these fire too to ensure all log messages
  /// are recorded.
  /// The Flush action is not implemented in this class, this is the responsibility of deriving classes.
  /// </summary>
  public abstract class BufferingLogger: Logger, IDisposable
  {
    #region Properties/Fields

    private Timer _flushBufferTimer;

    protected int BufferFlushSeconds { get; private set; }
    protected IList<ILogMessage> MessageBuffer { get; private set; }

    #endregion Properties/Fields

    #region ctors

    public BufferingLogger(string logName, int bufferFlushSeconds) : this(logName, LogMessageType.Info, bufferFlushSeconds) { }

    public BufferingLogger(string logName, LogMessageType minLogLevel, int bufferFlushSeconds = 15)
      : base(logName, minLogLevel)
    {
      this.ThrowIfArgumentNotPositive(() => bufferFlushSeconds);
      
      MessageBuffer = new List<ILogMessage>();
      BufferFlushSeconds = bufferFlushSeconds;

      MaintainBuffer();

      AppDomain.CurrentDomain.UnhandledException += OnApplicationShuttingDown;
      AppDomain.CurrentDomain.ProcessExit += OnApplicationShuttingDown;
    }

    #endregion ctors

    #region Event Handling

    protected virtual void OnApplicationShuttingDown(object sender, EventArgs e)
    {
      FlushAndCleanBuffer();
    }

    #endregion Event Handling

    #region Abstract

    public abstract void Flush();

    #endregion Abstract

    #region Methods

    private void FlushAndCleanBuffer()
    {
      Flush();
      MessageBuffer = new List<ILogMessage>();
    }

    private void MaintainBuffer()
    {
      _flushBufferTimer = new Timer();
      _flushBufferTimer.Interval = BufferFlushSeconds * 1000;
      _flushBufferTimer.Enabled = true;
      _flushBufferTimer.Elapsed += (s, e) =>
        {
          FlushAndCleanBuffer();
        };
      _flushBufferTimer.Start();
    }

    #endregion Methods

    #region Overrides

    protected override void WriteMessage(ILogMessage message)
    {
      MessageBuffer.Add(message);
    }

    public override void LogException(Exception exception)
    {
      base.LogException(exception);
      FlushAndCleanBuffer();
    }

    public override void LogException(Exception exception, string message, params object[] formatArgs)
    {
      base.LogException(exception, message, formatArgs);
      FlushAndCleanBuffer();
    }

    public override void LogException(Exception exception, string message)
    {
      base.LogException(exception, message);
      FlushAndCleanBuffer();
    }

    #endregion Overrides

    #region IDisposable

    public void Dispose()
    {
      if(_flushBufferTimer != null)
      {
        _flushBufferTimer.Stop();
        _flushBufferTimer.Dispose();
        _flushBufferTimer = null;
      }
    }

    #endregion IDisposable
  }
}
