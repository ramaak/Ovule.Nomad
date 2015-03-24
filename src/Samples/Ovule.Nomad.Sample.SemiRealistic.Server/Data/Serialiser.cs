using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Ovule.Nomad.Sample.SemiRealistic.Server.Data
{
  public class Serialiser
  {
    public static byte[] SerialiseToBytes(object toSerialise)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        new BinaryFormatter().Serialize(stream, toSerialise);
        stream.Position = 0;
        if (stream == null || stream.Length == 0)
          throw new Exception("Failed to serialise 'toSerialise'");
        byte[] result = stream.ToArray();
        return result;
      }
    }

    public static object DeserialiseBytes(byte[] toDeserialise)
    {
      using (MemoryStream stream = new MemoryStream(toDeserialise))
      {
        object result = new BinaryFormatter().Deserialize(stream);
        if (result == null)
          throw new Exception("Failed to deserialise 'toDeserialise'");
        return result;
      }
    }
  }
}
