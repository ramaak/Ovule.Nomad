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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;

namespace Ovule
{
  #region SerailsationException

  public class SerialisationException : Exception
  {
    public SerialisationException(string message) : base(message) { }
    public SerialisationException(string message, Exception innerException) : base(message, innerException) { }
  }

  #endregion SerailsationException

  public class Serialiser
  {
    #region BinaryFormatter

    public byte[] SerialiseToBytes(object toSerialise)
    {
      this.ThrowIfArgumentIsNull(() => toSerialise);

      using (MemoryStream stream = new MemoryStream())
      {
        new BinaryFormatter().Serialize(stream, toSerialise);
        stream.Position = 0;
        if (stream == null || stream.Length == 0)
          throw new SerialisationException("Failed to serialise 'toSerialise'");
        byte[] result = stream.ToArray();
        return result;
      }
    }

    public object DeserialiseBytes(byte[] toDeserialise)
    {
      this.ThrowIfArgumentIsNull(() => toDeserialise);

      using (MemoryStream stream = new MemoryStream(toDeserialise))
      {
        object result = new BinaryFormatter().Deserialize(stream);
        if (result == null)
          throw new SerialisationException("Failed to deserialise 'toDeserialise'");
        return result;
      }
    }

    public T DeserialiseBytes<T>(byte[] toDeserialise)
    {
      return (T)DeserialiseBytes(toDeserialise);
    }

    public string SerialiseToBase64(object toSerialise)
    {
      string result = Convert.ToBase64String(SerialiseToBytes(toSerialise));
      return result;
    }

    public IList<string> SerialiseToBase64(IEnumerable toSerialise)
    {
      List<string> serialisedItems = new List<string>();
      if (toSerialise != null)
      {
        foreach (object obj in toSerialise)
          serialisedItems.Add(SerialiseToBase64(obj));
      }
      return serialisedItems;
    }

    public object DeserialiseBase64(string toDeserialise)
    {
      this.ThrowIfArgumentIsNoValueString(() => toDeserialise);

      byte[] bytes = Convert.FromBase64String(toDeserialise);
      object result = DeserialiseBytes(bytes);
      return result;
    }

    public T DeserialiseBase64<T>(string toDeserialise)
    {
      return (T)DeserialiseBase64(toDeserialise);
    }

    public IList<T> DeserialiseBase64<T>(IList<string> serialisedItems, bool throwExceptionOnNullItem)
    {
      List<T> items = new List<T>();
      if (serialisedItems != null && serialisedItems.Count > 0)
      {
        foreach (string serialisedItem in serialisedItems)
        {
          T item = default(T);
          if (!string.IsNullOrWhiteSpace(serialisedItem))
            item = DeserialiseBase64<T>(serialisedItem);
          else if (throwExceptionOnNullItem)
            throw new NullReferenceException(string.Format("Null element which was meant to be of type '{0}' in collection to deserialise", typeof(T).FullName));
          items.Add(item);
        }
      }
      return items;
    }

    #endregion BinaryFormatter

    #region BinaryDataContract

    private static Dictionary<Type, DataContractSerializer> _dataContractSerialisers = new Dictionary<Type, DataContractSerializer>();

    private static DataContractSerializer GetDataContractSerialiser(Type type, IEnumerable<Type> knownTypes)
    {
      DataContractSerializer serialiser = null;
      if (_dataContractSerialisers.ContainsKey(type))
        serialiser = _dataContractSerialisers[type];
      else
      {
        serialiser = new DataContractSerializer(type, knownTypes);
        _dataContractSerialisers.Add(type, serialiser);
      }
      return serialiser;
    }

    public byte[] SerialiseDataContractToBytes<T>(T toSerialise)
    {
      return SerialiseDataContractToBytes(toSerialise, null, null);
    }

    public byte[] SerialiseDataContractToBytes<T>(T toSerialise, DataContractResolver resolver)
    {
      return SerialiseDataContractToBytes(toSerialise, null, resolver);
    }

    public byte[] SerialiseDataContractToBytes<T>(T toSerialise, IEnumerable<Type> knownTypes)
    {
      return SerialiseDataContractToBytes(toSerialise, knownTypes, null);
    }

    public byte[] SerialiseDataContractToBytes<T>(T toSerialise, IEnumerable<Type> knownTypes, DataContractResolver resolver)
    {
      this.ThrowIfArgumentIsNull(() => toSerialise);

      using (MemoryStream stream = new MemoryStream())
      {
        DataContractSerializer serialiser = GetDataContractSerialiser(typeof(T), knownTypes);
        using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
        {
          serialiser.WriteObject(writer, toSerialise, resolver);
          writer.Flush();

          stream.Position = 0;
          if (stream == null || stream.Length == 0)
            throw new SerialisationException("Failed to serialise 'toSerialise'");
          byte[] result = stream.ToArray();
          return result;
        }
      }
    }

    public T DeserialiseDataContractFromBytes<T>(byte[] toDeserialise)
    {
      return DeserialiseDataContractFromBytes<T>(toDeserialise, null, null);
    }

    public T DeserialiseDataContractFromBytes<T>(byte[] toDeserialise, DataContractResolver resolver)
    {
      return DeserialiseDataContractFromBytes<T>(toDeserialise, null, resolver);
    }

    public T DeserialiseDataContractFromBytes<T>(byte[] toDeserialise, IEnumerable<Type> knownTypes)
    {
      return DeserialiseDataContractFromBytes<T>(toDeserialise, knownTypes, null);
    }

    public T DeserialiseDataContractFromBytes<T>(byte[] toDeserialise, IEnumerable<Type> knownTypes, DataContractResolver resolver)
    {
      this.ThrowIfArgumentIsNull(() => toDeserialise);

      using (MemoryStream stream = new MemoryStream(toDeserialise))
      {
        DataContractSerializer serialiser = GetDataContractSerialiser(typeof(T), knownTypes);
        using (XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
        {
          object result = serialiser.ReadObject(reader, false, resolver);
          if (result == null)
            throw new SerialisationException("Failed to deserialise 'toDeserialise'");
          return (T)result;
        }
      }
    }

    public string SerialiseDataContractToBase64<T>(T toSerialise)
    {
      return SerialiseDataContractToBase64(toSerialise, null, null);
    }

    public string SerialiseDataContractToBase64<T>(T toSerialise, DataContractResolver resolver)
    {
      return SerialiseDataContractToBase64(toSerialise, null, resolver);
    }

    public string SerialiseDataContractToBase64<T>(T toSerialise, IEnumerable<Type> knownTypes)
    {
      return SerialiseDataContractToBase64(toSerialise, knownTypes, null);
    }

    public string SerialiseDataContractToBase64<T>(T toSerialise, IEnumerable<Type> knownTypes, DataContractResolver resolver)
    {
      string result = Convert.ToBase64String(SerialiseDataContractToBytes(toSerialise, knownTypes, resolver));
      return result;
    }

    public T DeserialiseDataContractFromBase64<T>(string toDeserialise, DataContractResolver resolver)
    {
      return DeserialiseDataContractFromBase64<T>(toDeserialise, null, resolver);
    }

    public T DeserialiseDataContractFromBase64<T>(string toDeserialise, IEnumerable<Type> knownTypes)
    {
      return DeserialiseDataContractFromBase64<T>(toDeserialise, knownTypes, null);
    }

    public T DeserialiseDataContractFromBase64<T>(string toDeserialise, IEnumerable<Type> knownTypes, DataContractResolver resolver)
    {
      this.ThrowIfArgumentIsNoValueString(() => toDeserialise);

      byte[] bytes = Convert.FromBase64String(toDeserialise);
      T result = DeserialiseDataContractFromBytes<T>(bytes, knownTypes, resolver);
      return result;
    }

    #endregion BinaryDataContract
  }
}
