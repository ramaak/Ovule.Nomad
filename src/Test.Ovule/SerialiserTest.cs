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
using System.Collections;
using System.Collections.Generic;

namespace Test.Ovule
{
  [TestClass]
  public class SerialiserTest
  {
    #region Test Util

    private TestSerialisationObject GetTestObjectA()
    {
      TestSerialisationObject test = new TestSerialisationObject()
      {
        IntProp = 56465,
        LongProp = 86545646,
        DoubleProp = 145464.586867,
        DecimalProp = 5787645.6455m,
        StringProp = "jkljldflkjlkjlsd\tfpp[oiiouefspo[l;lkd;olkfsdf</.,.>>lkklkj dfj lj df*^&*^%%^$%^900-9-0897^$%^$`",
        DateProp = DateTime.Now,
        ListProp = new List<int>() { 4, 5, 6, 7, 874896, 999, 10120, 21212121 },
        ObjectProp = new Tuple<int, string>(887, "kljijioudf,<.flokjldd/>ojlkjdl  ldsf"),
        Self = GetTestObjectB(),
      };
      return test;
    }

    private TestSerialisationObject GetTestObjectB()
    {
      TestSerialisationObject test = new TestSerialisationObject()
      {
        IntProp = 111111,
        LongProp = 86545646,
        DoubleProp = 145464.586867,
        DecimalProp = 5787645.6455m,
        StringProp = "jkljldflkjlkjlsd\tfpp[oiiouefspo[l;lkd;olkfsdf</.,.>>lkklkj dfj lj df*^&*^%%^$%^900-9-0897^$%^$`",
        DateProp = DateTime.Now,
        ListProp = new List<int>() { 2134, 54564, 8878, 681412, 0, 53, 664789, 45 },
        ObjectProp = new Tuple<int, string>(887, "kljijioudf,<.flokjldd/>ojlkjdl  ldsf"),
        Self = new TestSerialisationObject(),
      };
      return test;
    }

    #endregion Test Util

    #region BinaryFormatter

    [TestMethod]
    public void SerialiseToBytes()
    {
      TestSerialisationObject origObj = GetTestObjectA();
      byte[] bytes = new Serialiser().SerialiseToBytes(origObj);

      TestSerialisationObject deserObj = (TestSerialisationObject)new Serialiser().DeserialiseBytes(bytes);

      Assert.AreEqual(origObj, deserObj);
    }

    [TestMethod]
    public void DeserialiseBytes()
    {
      SerialiseToBytes();
    }

    [TestMethod]
    public void DeserialiseBytes<T>()
    {
      TestSerialisationObject origObj = GetTestObjectA();
      byte[] bytes = new Serialiser().SerialiseToBytes(origObj);

      TestSerialisationObject deserObj = new Serialiser().DeserialiseBytes<TestSerialisationObject>(bytes);

      Assert.AreEqual(origObj, deserObj);
    }

    [TestMethod]
    public void SerialiseToBase64()
    {
      TestSerialisationObject origObj = GetTestObjectA();
      string base64 = new Serialiser().SerialiseToBase64(origObj);

      TestSerialisationObject deserObj = (TestSerialisationObject)new Serialiser().DeserialiseBase64(base64);

      Assert.AreEqual(origObj, deserObj);
    }

    [TestMethod]
    public void SerialiseToBase64List()
    {
      List<TestSerialisationObject> items = new List<TestSerialisationObject>()
      {
        GetTestObjectA(),
        GetTestObjectB(),
        GetTestObjectB(),
        GetTestObjectB(),
      };
      IList<string> base64 = new Serialiser().SerialiseToBase64(items);
      IList<TestSerialisationObject> deserItems = new Serialiser().DeserialiseBase64<TestSerialisationObject>(base64, true);

      Assert.IsTrue(items.Count == deserItems.Count);

      for (int i = 0; i < items.Count; i++)
        Assert.AreEqual(items[i], deserItems[i]);
    }

    [TestMethod]
    public void DeserialiseBase64()
    {
      SerialiseToBase64();
    }

    [TestMethod]
    public void DeserialiseBase64<T>()
    {
      SerialiseToBase64();
    }

    #endregion BinaryFormatter

    #region TestSerialisationObject

    [Serializable]
    private class TestSerialisationObjectBase
    {
      public int IntProp { get; set; }
      public long LongProp { get; set; }
      public double DoubleProp { get; set; }
      public decimal DecimalProp { get; set; }
      public DateTime DateProp { get; set; }
      public string StringProp { get; set; }

      public override int GetHashCode()
      {
        return base.GetHashCode();
      }

      public override bool Equals(object obj)
      {
        TestSerialisationObjectBase that = obj as TestSerialisationObjectBase;
        if (that != null)
        {
          if (object.ReferenceEquals(this, that))
            return true;

          return
            this.IntProp == that.IntProp &&
            this.LongProp == that.LongProp &&
            this.DoubleProp == that.DoubleProp &&
            this.DecimalProp == that.DecimalProp &&
            this.DateProp == that.DateProp &&
            this.StringProp == that.StringProp;
        }
        return false;
      }
    }

    [Serializable]
    private class TestSerialisationObject : TestSerialisationObjectBase
    {
      public IList<int> ListProp { get; set; }
      public object ObjectProp { get; set; }
      public TestSerialisationObject Self { get; set; }

      public override int GetHashCode()
      {
        return base.GetHashCode();
      }

      public override bool Equals(object obj)
      {
        if (base.Equals(obj))
        {
          TestSerialisationObject that = obj as TestSerialisationObject;
          if (that != null)
          {
            if ((this.ListProp == null && that.ListProp == null) ||
              (this.ListProp.Count == that.ListProp.Count))
            {
              if (this.ListProp != null)
              {
                for (int i = 0; i < this.ListProp.Count; i++)
                {
                  if (this.ListProp[i] != that.ListProp[i])
                    return false;
                }
              }
              if ((this.ObjectProp == null && that.ObjectProp == null) || this.ObjectProp.Equals(that.ObjectProp))
                return (this.Self == null && that.Self == null) || this.Self.Equals(that.Self);
            }
          }
        }
        return false;
      }
    }

    #endregion TestSerialisationObject
  }
}
