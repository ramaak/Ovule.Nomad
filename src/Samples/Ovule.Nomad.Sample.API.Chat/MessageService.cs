using System.Collections.ObjectModel;
using System.Windows;

namespace Ovule.Nomad.Sample.API.Chat
{
  public class MessageService
  {
    [NomadIgnore]
    public static ObservableCollection<ChatMessage> ChatMessages { get; private set; }

    [NomadIgnore]
    public static ObservableCollection<ChatFile> ReceviedFiles { get; private set; }

    static MessageService()
    {
      ChatMessages = new ObservableCollection<ChatMessage>();
      ReceviedFiles = new ObservableCollection<ChatFile>();
    }

    public static void SendMessage(ChatMessage message)
    {
      ChatMessages.Add(message);
    }

    public static void SendFile(ChatFile file)
    {
      MessageBoxResult res = MessageBox.Show(string.Format("'{0}' wants to send you a file, accept?", file.From.Id), "File Transfer", 
        MessageBoxButton.YesNo, MessageBoxImage.Question);

      if (res == MessageBoxResult.Yes)
        ReceviedFiles.Add(file);
    }
  }
}
