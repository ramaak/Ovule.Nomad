using System;

namespace Ovule.Nomad.Sample.Chat
{
  [Serializable]
  public class ChatFile
  {
    public string Name { get; private set; }
    public byte[] Content { get; private set; }
    public User From { get; private set; }

    public ChatFile(User from, string name, byte[] content)
    {
      From = from;
      Name = name;
      Content = content;
    }
  }
}
