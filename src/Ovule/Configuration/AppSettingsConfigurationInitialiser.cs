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
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;

namespace Ovule.Configuration
{
  /// <summary>
  /// This implementation of IConfigurationInitialiser works with .Net settings files, i.e. App.config and Web.config.
  /// It matches settings in the configuration file with properties in type T and returns a populated instance of T.
  /// </summary>
  public class AppSettingsConfigurationInitialiser<T> : IConfigurationInitialiser<T> where T : IConfigurationCollection, new()
  {
    /// <summary>
    /// Takes an IConfigurationCollection and populates it with the values in the applications configuration file.
    /// </summary>
    /// <param name="configurationCollection"></param>
    public T Initialise(System.Configuration.Configuration config)
    {
      T configurationCollection = new T();
      IList<string> errors = new List<string>();

      PropertyInfo[] settingsCollectionProps = typeof(T).GetProperties();
      foreach (PropertyInfo prop in settingsCollectionProps)
      {
        if (prop.GetSetMethod() == null)
          continue;

        string value = config.AppSettings.Settings[prop.Name] == null ? null : config.AppSettings.Settings[prop.Name].Value;
        if (value == null && configurationCollection.AreAllSettingsRequired)
          errors.Add(string.Format("No value specified for required configuration setting called '{0}'", prop.Name));

        if (prop.PropertyType.Equals(typeof(bool)))
          prop.SetValue(configurationCollection, Convert.ToBoolean(value), null);
        else if (prop.PropertyType.Equals(typeof(short)))
          prop.SetValue(configurationCollection, Convert.ToInt16(value), null);
        else if (prop.PropertyType.Equals(typeof(int)))
          prop.SetValue(configurationCollection, Convert.ToInt32(value), null);
        else if (prop.PropertyType.Equals(typeof(long)))
          prop.SetValue(configurationCollection, Convert.ToInt64(value), null);
        else if (prop.PropertyType.Equals(typeof(decimal)))
          prop.SetValue(configurationCollection, Convert.ToDecimal(value), null);
        else if (prop.PropertyType.Equals(typeof(double)))
          prop.SetValue(configurationCollection, Convert.ToDouble(value), null);
        else if (prop.PropertyType.Equals(typeof(char)))
          prop.SetValue(configurationCollection, Convert.ToChar(value), null);
        else if (prop.PropertyType.Equals(typeof(string)))
          prop.SetValue(configurationCollection, Convert.ToString(value), null);
        else if (prop.PropertyType.Equals(typeof(TimeSpan)))
          prop.SetValue(configurationCollection, TimeSpan.Parse(value), null);
        else
          errors.Add(string.Format("Unexpected configuration property type of {0} for property {1}", prop.PropertyType.FullName, prop.Name));
      }

      if (errors.Count == 0)
        errors = configurationCollection.GetValidationErrors();

      if (errors != null && errors.Count > 0)
      {
        string errorString = "";
        foreach (string error in errors)
          errorString += string.Format("{0}\r\n", error);
        throw new ConfigurationException(string.Format("The following error(s) occurred reading configuration information for {0}:\r\n{1}", configurationCollection.GetType().Name, errorString));
      }
      return configurationCollection;
    }
  }
}
