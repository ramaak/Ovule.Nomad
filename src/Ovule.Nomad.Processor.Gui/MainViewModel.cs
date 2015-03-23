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
using Ovule.Nomad.Client;
using Ovule.Nomad.Client.Email;
using Ovule.Nomad.Server;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;

namespace Ovule.Nomad.Processor.Gui
{
  public class MainViewModel : ViewModel
  {
    #region Properties/Fields

    private string _applicationDirectory;
    public string ApplicationDirectory
    {
      get { return _applicationDirectory; }
      set { if (_applicationDirectory != value) { _applicationDirectory = value; NotifyPropertyChanged(() => ApplicationDirectory); } }
    }

    private string _serverDirectory;
    public string ServerDirectory
    {
      get { return _serverDirectory; }
      set { if (_serverDirectory != value) { _serverDirectory = value; NotifyPropertyChanged(() => ServerDirectory); } }
    }

    private string _ProcessDetails;
    public string ProcessDetails
    {
      get { return _ProcessDetails; }
      set { if (_ProcessDetails != value) { _ProcessDetails = value; NotifyPropertyChanged(() => ProcessDetails); } }
    }

    private ObservableCollection<NetworkCommunicationsNomadClientMapping> _networkCommunicationsTypes;
    public ObservableCollection<NetworkCommunicationsNomadClientMapping> NetworkCommunicationsTypes
    {
      get { return _networkCommunicationsTypes; }
      set { if (_networkCommunicationsTypes != value) { _networkCommunicationsTypes = value; NotifyPropertyChanged(() => NetworkCommunicationsTypes); } }
    }

    private NetworkCommunicationsNomadClientMapping _selectedNetworkCommunicationsType;
    public NetworkCommunicationsNomadClientMapping SelectedNetworkCommunicationsType
    {
      get { return _selectedNetworkCommunicationsType; }
      set { if (_selectedNetworkCommunicationsType != value) { _selectedNetworkCommunicationsType = value; NotifyPropertyChanged(() => SelectedNetworkCommunicationsType); } }
    }

    private ServiceUri _ServiceUri;
    public ServiceUri ServiceUri
    {
      get { return _ServiceUri; }
      set { if (_ServiceUri != value) { _ServiceUri = value; NotifyPropertyChanged(() => ServiceUri); } }
    }

    #endregion Properties/Fields

    #region ctors

    public MainViewModel()
    {
      NetworkCommunicationsTypes = new ObservableCollection<NetworkCommunicationsNomadClientMapping>()
      {
        new NetworkCommunicationsNomadClientMapping(NetworkCommunicationsType.Http, typeof(NomadWcfClient)),
        new NetworkCommunicationsNomadClientMapping(NetworkCommunicationsType.Tcp, typeof(NomadWcfClient)),
        new NetworkCommunicationsNomadClientMapping(NetworkCommunicationsType.NamedPipes, typeof(NomadWcfClient)),
        new NetworkCommunicationsNomadClientMapping(NetworkCommunicationsType.Email, typeof(NomadEmailClient)),
      };
      PropertyChanged += OnPropertyChanged;

      SelectedNetworkCommunicationsType = NetworkCommunicationsTypes[0];
    }

    #endregion ctors

    #region Event Handling

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "SelectedNetworkCommunicationsType")
      {
        if (SelectedNetworkCommunicationsType != null)
          ServiceUri = GetDefaultServiceUri(SelectedNetworkCommunicationsType.NetworkCommsType);
        else
          ServiceUri = null;
      }
    }

    #endregion Event Handling

    #region Commands

    private ICommand _selectApplicationDirectoryCommand;
    public ICommand SelectApplicationDirectoryCommand
    {
      get
      {
        if (_selectApplicationDirectoryCommand == null)
          _selectApplicationDirectoryCommand = new RelayCommand(DoSelectApplicationDirectory, () => CanSelectApplicationDirectory);
        return _selectApplicationDirectoryCommand;
      }
    }
    private bool CanSelectApplicationDirectory { get { return true; } }
    private void DoSelectApplicationDirectory()
    {
      if (CanSelectApplicationDirectory)
        ApplicationDirectory = SelectDirectory();
    }

    private ICommand _selectServerDirectoryCommand;
    public ICommand SelectServerDirectoryCommand
    {
      get
      {
        if (_selectServerDirectoryCommand == null)
          _selectServerDirectoryCommand = new RelayCommand(DoSelectServerDirectory, () => CanSelectServerDirectory);
        return _selectServerDirectoryCommand;
      }
    }
    private bool CanSelectServerDirectory { get { return true; } }
    private void DoSelectServerDirectory()
    {
      if (CanSelectServerDirectory)
        ServerDirectory = SelectDirectory();
    }

    private ICommand _processCommand;
    public ICommand ProcessCommand
    {
      get
      {
        if (_processCommand == null)
          _processCommand = new RelayCommand(DoProcess, () => CanProcess);
        return _processCommand;
      }
    }
    private bool CanProcess { get { return SelectedNetworkCommunicationsType != null && !string.IsNullOrWhiteSpace(ApplicationDirectory) && !string.IsNullOrWhiteSpace(ServerDirectory); } }
    private void DoProcess()
    {
      if (CanProcess)
      {
        ProcessDetails = null;

        if (!Directory.Exists(ApplicationDirectory))
          MessageBox.Show("The application directory does not exist", "Path Not Found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
        else
        {
          ApplicationProcessor processor = new ApplicationProcessor();
          processor.AssemblyProcessed += (s, e) =>
            {
              ProcessDetails += string.Format("{0} {1} {2}{3}", e.ContainsNomadMethods ? "+" : "-", e.AssemblyName, e.ProcessingError, Environment.NewLine);
              System.Windows.Forms.Application.DoEvents();
            };
          processor.Process(SelectedNetworkCommunicationsType.NomadClientType, ApplicationDirectory, ServerDirectory);
          
          ConfigurationProcessor configProcessor = new ConfigurationProcessor();

          string clientNomadAsmName = string.Format("{0}.dll", typeof(INomadClient).Assembly.GetName().Name);
          string serverNomadAsmName = string.Format("{0}.dll", typeof(INomadServer).Assembly.GetName().Name);
          string clientConfigFilePath = Path.Combine(ApplicationDirectory, clientNomadAsmName);
          string serverConfigFilePath = Path.Combine(ServerDirectory, serverNomadAsmName);

          configProcessor.Process(clientConfigFilePath, ServiceUri.StringValue);
          ProcessDetails += string.Format("Client configured: {0}{1}", clientConfigFilePath, Environment.NewLine);

          configProcessor.Process(serverConfigFilePath, ServiceUri.StringValue);
          ProcessDetails += string.Format("Server configured: {0}{1}", serverConfigFilePath, Environment.NewLine);
        }
      }
    }

    private ICommand _closeCommand;
    public ICommand CloseCommand
    {
      get
      {
        if (_closeCommand == null)
          _closeCommand = new RelayCommand(DoClose, () => CanClose);
        return _closeCommand;
      }
    }
    private bool CanClose { get { return true; } }
    private void DoClose()
    {
      if (CanClose)
        System.Windows.Application.Current.Shutdown();
    }

    #endregion Commands

    #region Methods

    private ServiceUri GetDefaultServiceUri(NetworkCommunicationsType commsType)
    {
      ServiceUri result = null;
      if (commsType == NetworkCommunicationsType.Http)
        result = new ServiceUri(NetworkCommunicationsType.Http, "localhost", (int)commsType, "Ovule.Nomad.Server");
      else if (commsType == NetworkCommunicationsType.Tcp)
        result = new ServiceUri(NetworkCommunicationsType.Tcp, "localhost", (int)commsType, null);
      else if (commsType == NetworkCommunicationsType.NamedPipes)
        result = new ServiceUri(NetworkCommunicationsType.NamedPipes, "localhost", null, null);
      else if (commsType == NetworkCommunicationsType.Email)
        result = new ServiceUri(NetworkCommunicationsType.Email, "yourdomain.com", null, "serice.user");
      else
        throw new NomadException("Unexpected client type of '{0}'", commsType.ToString());

      return result;
    }

    private string SelectDirectory()
    {
      FolderBrowserDialog dlg = new FolderBrowserDialog();
      dlg.ShowNewFolderButton = false;
      DialogResult res = dlg.ShowDialog();
      if (res == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
        return dlg.SelectedPath;
      return null;
    }

    #endregion Methods
  }
}
