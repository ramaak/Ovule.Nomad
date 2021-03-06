﻿using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Ovule.Nomad.Sample.Chat
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    #region Properties

    private User _LocalUser;
    public User LocalUser
    {
      get { return _LocalUser; }
      set { if (_LocalUser != value) { _LocalUser = value; NotifyPropertyChanged("LocalUser"); } }
    }

    private ObservableCollection<User> _SignedInUsers;
    public ObservableCollection<User> SignedInUsers
    {
      get { return _SignedInUsers; }
      set { if (_SignedInUsers != value) { _SignedInUsers = value; NotifyPropertyChanged("SignedInUsers"); } }
    }

    private User _SelectedUser;
    public User SelectedUser
    {
      get { return _SelectedUser; }
      set { if (_SelectedUser != value) { _SelectedUser = value; NotifyPropertyChanged("SelectedUser"); } }
    }

    private string _WorkingMessageValue;
    public string WorkingMessageValue
    {
      get { return _WorkingMessageValue; }
      set { if (_WorkingMessageValue != value) { _WorkingMessageValue = value; NotifyPropertyChanged("WorkingMessageValue"); } }
    }

    #endregion Properties

    #region ctors

    public MainWindow() { }

    public MainWindow(User localUser)
    {
      InitializeComponent();

      LocalUser = localUser;
      UserService.SignIn(LocalUser);
      DispatcherTimer timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(5) };
      timer.Tick += OnTimerTick;
      timer.Start();
      this.Closing += (s, e) => { timer.Stop(); };

      this.DataContext = this;
    }

    #endregion ctors

    #region Event Handling

    private void OnTimerTick(object sender, EventArgs e)
    {
      try
      {
        string selectedUserId = null;
        if (SelectedUser != null)
          selectedUserId = SelectedUser.Id;
        SignedInUsers = new ObservableCollection<User>(new UserService().GetSignedInUsers().Where(u => !u.Equals(LocalUser)));
        if (SignedInUsers != null && selectedUserId != null)
          SelectedUser = SignedInUsers.FirstOrDefault(u => u.Id == selectedUserId);
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
    }

    #endregion Event Handling

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion INotifyPropertyChanged

    #region Methods

    private void Send_Click(object sender, RoutedEventArgs e)
    {
      string errors = string.Empty;
      if (string.IsNullOrWhiteSpace(WorkingMessageValue))
        errors += "You must enter a message\r\n";
      if (SelectedUser == null)
        errors += "You must select a user\r\n";
      if (errors != string.Empty)
        MessageBox.Show(string.Format("Please address the following issues:\r\n{0}", errors));
      else
      {
        MessageService.SendMessage(new ShippingContainer<ChatMessage>(SelectedUser.Uri, new ChatMessage(LocalUser, WorkingMessageValue)));
        WorkingMessageValue = null;
      }
    }

    private void SendFile_Click(object sender, RoutedEventArgs e)
    {
      if (lstSignedInUsers.SelectedItem == null)
        MessageBox.Show("You must select a user");
      else
      {
        OpenFileDialog openDlg = new OpenFileDialog();
        if (openDlg.ShowDialog().GetValueOrDefault(false))
        {
          byte[] fileContent = System.IO.File.ReadAllBytes(openDlg.FileName);
          ChatFile toSend = new ChatFile(LocalUser, System.IO.Path.GetFileName(openDlg.FileName), fileContent);
          MessageService.SendFile(new ShippingContainer<ChatFile>(SelectedUser.Uri, toSend));
        }
      }
    }

    private void ReceivedFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (lstReceivedFiles.SelectedItem is ChatFile)
      {
        ChatFile receivedFile = (ChatFile)lstReceivedFiles.SelectedItem;
        string tmpFilename = Path.Combine(Path.GetTempPath(), receivedFile.Name);
        File.WriteAllBytes(tmpFilename, receivedFile.Content);
        Process.Start(tmpFilename);
      }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      base.OnClosing(e);
      UserService.SignOut(LocalUser);
    }

    #endregion Methods
  }
}

