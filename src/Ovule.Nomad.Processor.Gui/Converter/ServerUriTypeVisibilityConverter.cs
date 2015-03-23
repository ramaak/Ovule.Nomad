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
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ovule.Nomad.Processor.Gui.Converter
{
  public class ServerUriTypeVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is NetworkCommunicationsType && parameter is NetworkCommunicationsType)
      {
        if ((NetworkCommunicationsType)value == (NetworkCommunicationsType)parameter)
          return Visibility.Visible;
        return Visibility.Collapsed;
      }
      throw new ArgumentException(string.Format("Expected both 'value' and 'parameter' to be of type '{0}'", typeof(NetworkCommunicationsType).FullName));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
