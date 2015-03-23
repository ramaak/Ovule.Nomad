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
using System.IO;

namespace Ovule.Nomad.Processor.Gui
{
  public class ConfigurationProcessor
  {
    public void Process(string nomadAssemblyPath, string serverUri)
    {
      this.ThrowIfArgumentIsNoValueString(() => nomadAssemblyPath);

      if (!File.Exists(nomadAssemblyPath))
        throw new FileNotFoundException(string.Format("Expected Nomad assembly to be at location '{0}' but it wasn't found", nomadAssemblyPath));

      string configFilePath = string.Format("{0}.config", nomadAssemblyPath);
      if (File.Exists(configFilePath))
        UpdateConfigFile(configFilePath, serverUri);
      else
        CreateConfigFile(configFilePath, serverUri);
    }

    private void CreateConfigFile(string configFilePath, string serverUri)
    {
      string configFileContent = string.Format(
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine +
        "<configuration>" + Environment.NewLine +
        "  <appSettings>" + Environment.NewLine +
        "    <add key=\"NomadServerUri\" value=\"{0}\"/>" + Environment.NewLine +
        "  </appSettings>" + Environment.NewLine +
        "</configuration>", serverUri);
      File.WriteAllText(configFilePath, configFileContent);
    }

    private void UpdateConfigFile(string configFilePath, string serverUri)
    {
      bool updatedServerUri = false;
      string alteredFileContent = string.Empty;
      string[] lines = File.ReadAllLines(configFilePath);
      if(lines != null)
      {
        foreach(string line in lines)
        {
          if (line.Contains("key=\"NomadServerUri\""))
          {
            alteredFileContent += string.Format("    <add key=\"NomadServerUri\" value=\"{0}\"/>{1}", serverUri, Environment.NewLine);
            updatedServerUri = true;
          }
          else
            alteredFileContent += string.Format("{0}{1}", line, Environment.NewLine);
        }
        File.WriteAllText(configFilePath, alteredFileContent);
      }
      if (!updatedServerUri)
        throw new InvalidOperationException(string.Format("A configuration file was found at '{0}' however it did not contain a line for 'NomadServerUri' and so it couldn't be updated", configFilePath));
    }
  }
}
