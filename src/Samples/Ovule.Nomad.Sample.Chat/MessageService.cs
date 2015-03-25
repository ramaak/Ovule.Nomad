using System.Collections.ObjectModel;
using System.Windows;

namespace Ovule.Nomad.Sample.Chat
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

    [NomadMethod(NomadMethodType.Repeat, true)]
    public static void SendMessage(IShippingContainer<ChatMessage> message)
    {
      ChatMessages.Add(message.Cargo);
    }

    [NomadMethod(NomadMethodType.Relay, true)]
    public static void SendFile(IShippingContainer<ChatFile> file)
    {
      MessageBoxResult res = MessageBox.Show(string.Format("'{0}' wants to send you a file, accept?", file.Cargo.From.Id), "File Transfer", 
        MessageBoxButton.YesNo, MessageBoxImage.Question);

      if (res == MessageBoxResult.Yes)
        ReceviedFiles.Add(file.Cargo);
    }
  }
}
