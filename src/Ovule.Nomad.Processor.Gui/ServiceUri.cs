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
using System.ComponentModel;

namespace Ovule.Nomad.Processor.Gui
{
  public class ServiceUri: PropertyChangeNotifier
  {
    #region Properties/Fields

    private NetworkCommunicationsType _serviceType;
    public NetworkCommunicationsType ServiceType
    {
      get { return _serviceType; }
      set { if (_serviceType != value) { _serviceType = value; NotifyPropertyChanged(() => ServiceType); } }
    }

    private string _scheme;
    public string Scheme
    {
      get { return _scheme; }
      set { if (_scheme != value) { _scheme = value; NotifyPropertyChanged(() => Scheme); } }
    }

    private string _server;
    public string Server
    {
      get { return _server; }
      set { if (_server != value) { _server = value; NotifyPropertyChanged(() => Server); } }
    }

    private int? _port;
    public int? Port
    {
      get { return _port; }
      set { if (_port != value) { _port = value; NotifyPropertyChanged(() => Port); } }
    }

    private string _path;
    public string Path
    {
      get { return _path; }
      set { if (_path != value) { _path = value; NotifyPropertyChanged(() => Path); } }
    }

    private string _serviceName;
    public string ServiceName
    {
      get { return _serviceName; }
      set { if (_serviceName != value) { _serviceName = value; NotifyPropertyChanged(() => ServiceName); } }
    }

    private string _stringValue;
    public string StringValue
    {
      get { return _stringValue; }
      set { if (_stringValue != value) { _stringValue = value; NotifyPropertyChanged(() => StringValue); } }
    }

    #endregion Properties/Fields

    #region ctors

    public ServiceUri(NetworkCommunicationsType serviceType, string server, int? port, string path)
    {
      ServiceType = serviceType;
      Server = server;
      Port = port;
      Path = path;
      Scheme = GetScheme();
      ServiceName = GetServiceName();

      StringValue = ToString();
      PropertyChanged += (s, e) => { StringValue = ToString(); };
    }

    #endregion ctors

    #region Methods

    public string GetScheme()
    {
      switch (ServiceType)
      {
        case NetworkCommunicationsType.Http: return "http://";
        case NetworkCommunicationsType.Tcp: return "net.tcp://";
        case NetworkCommunicationsType.NamedPipes: return "net.pipe://";
        case NetworkCommunicationsType.Email: return "mailto://";
        default: return "UNKNOWN";
      }
    }

    public string GetServiceName()
    {
      string serviceName = "NomadService";
      //just worrying about self-hosted services for now
      //if (ServiceType == NetworkCommunicationsType.Http)
      //  serviceName += ".svc";
      if (ServiceType == NetworkCommunicationsType.Email)
        serviceName = string.Empty;
      return serviceName;
    }

    public override string ToString()
    {
      switch (ServiceType)
      {
        case NetworkCommunicationsType.Http: return string.Format("{0}{1}:{2}/{3}/{4}", GetScheme(), Server, Port, Path, GetServiceName());
        case NetworkCommunicationsType.Tcp: return string.Format("{0}{1}:{2}/{3}", GetScheme(), Server, Port, GetServiceName());
        case NetworkCommunicationsType.NamedPipes: return string.Format("{0}{1}/{2}", GetScheme(), Server, GetServiceName());
        case NetworkCommunicationsType.Email: return string.Format("{0}{1}@{2}", GetScheme(), Path, Server);
        default: return "UNKNOWN";
      }
    }

    #endregion Methods
  }
}
