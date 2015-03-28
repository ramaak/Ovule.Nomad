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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ovule.Diagnostics;
using System;
using System.IO;
using System.Threading;

namespace Test.Ovule.Diagnostics
{
  [TestClass]
  public class BufferingTextFileLoggerTest
  {
    #region Test Util

    private BufferingTextFileLogger GetLogger()
    {
      BufferingTextFileLogger logger = 
        new BufferingTextFileLogger("c:\\temp\\bufferingloggertest", "XX-XXTestLogXX-XX", LogMessageType.Info, 1);
      if (File.Exists(logger.LogFilePath))
        File.Delete(logger.LogFilePath);
      return logger;
    }

    private void DeleteFile(string filename)
    {
      File.Delete(filename);
    }

    private int GetFileLineCount(string filename)
    {
      return File.ReadAllLines(filename).Length;
    }

    #endregion Test Util

    #region Tests

    [TestMethod]
    public void LogInfo()
    {
      int testLineCount = new Random().Next(100, 500);
      BufferingTextFileLogger logger = GetLogger();
      for (int i = 0; i < testLineCount; i++)
        logger.LogInfo("This is test message {0}", i);

      Thread.Sleep(1250);//wait for buffer to flush
      Assert.AreEqual(testLineCount, GetFileLineCount(logger.LogFilePath));
      DeleteFile(logger.LogFilePath);
    }

    [TestMethod]
    public void LogWarning(string message)
    {
      int testLineCount = new Random().Next(100, 500);
      BufferingTextFileLogger logger = GetLogger();
      for (int i = 0; i < testLineCount; i++)
        logger.LogWarning("This is test warning {0}", i);

      Thread.Sleep(1250);//wait for buffer to flush
      Assert.AreEqual(testLineCount, GetFileLineCount(logger.LogFilePath));
      DeleteFile(logger.LogFilePath);
    }

    [TestMethod]
    public void LogError(string message)
    {
      int testLineCount = new Random().Next(100, 500);
      BufferingTextFileLogger logger = GetLogger();
      for (int i = 0; i < testLineCount; i++)
        logger.LogError("This is test error {0}", i);

      Thread.Sleep(1250);//wait for buffer to flush
      Assert.AreEqual(testLineCount, GetFileLineCount(logger.LogFilePath));
      DeleteFile(logger.LogFilePath);
    }

    [TestMethod]
    public void LogException(Exception ex, string message)
    {
      int testLineCount = new Random().Next(100, 500);
      BufferingTextFileLogger logger = GetLogger();
      for (int i = 0; i < testLineCount; i++)
        logger.LogException(new Exception("test ex"), "This is test exception message {0}", i);

      Thread.Sleep(150);//wait for buffer to flush
      Assert.IsTrue(testLineCount <= GetFileLineCount(logger.LogFilePath));
      DeleteFile(logger.LogFilePath);
    }

    #endregion Tests
  }
}
