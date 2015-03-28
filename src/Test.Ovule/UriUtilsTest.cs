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
using Ovule;
using System;

namespace Test.Ovule
{
  [TestClass]
  public class UriUtilsTest
  {
    [TestMethod]
    public void GetUriType()
    {
      Uri httpUri = new Uri("http://www.test.com");
      Uri tcpUri = new Uri("net.tcp://192.168.0.1");
      Uri namedPipeUri = new Uri("net.pipe://192.168.0.1");
      Uri mailtoUri = new Uri("mailto://test@test.com");
      Uri invalidUri = new Uri("xyz://www.test.com");

      Assert.AreEqual(UriType.Http, UriUtils.GetType(httpUri));
      Assert.AreEqual(UriType.Tcp, UriUtils.GetType(tcpUri));
      Assert.AreEqual(UriType.NamedPipe, UriUtils.GetType(namedPipeUri));
      Assert.AreEqual(UriType.Email, UriUtils.GetType(mailtoUri));

      bool isExThrown = false;
      try
      {
        UriUtils.GetType(invalidUri);
      }
      catch
      {
        isExThrown = true;
      }
      Assert.IsTrue(isExThrown);
    }
  }
}
