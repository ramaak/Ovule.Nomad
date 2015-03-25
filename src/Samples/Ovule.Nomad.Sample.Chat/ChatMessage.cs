using System;

namespace Ovule.Nomad.Sample.Chat
{
  [Serializable]
  public class ChatMessage
  {
    public string Message { get; private set; }
    public User From { get; private set; }

    public ChatMessage(User from, string message)
    {
      From = from;
      Message = message;
    }
  }
}
