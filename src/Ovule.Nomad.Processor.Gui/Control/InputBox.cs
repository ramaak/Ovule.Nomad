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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ovule.Nomad.Processor.Gui.Control
{
  public enum InputType { String, Integer }

  public class InputBox : TextBox
  {
    public static DependencyProperty InputTypeProperty = DependencyProperty.Register("InputType", typeof(InputType), typeof(InputBox));

    public InputType InputType
    {
      get { return (InputType)GetValue(InputTypeProperty); }
      set { SetValue(InputTypeProperty, value); }
    }

    static InputBox()
    {
      PropertyMetadata defaultMetadata = TextBox.TextProperty.GetMetadata(typeof(TextBox));

      TextBox.TextProperty.OverrideMetadata(typeof(InputBox), new FrameworkPropertyMetadata(
          string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          defaultMetadata.PropertyChangedCallback, defaultMetadata.CoerceValueCallback, true,
          System.Windows.Data.UpdateSourceTrigger.PropertyChanged));
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
      base.OnPropertyChanged(e);
      if (InputType == Control.InputType.Integer && e.Property.Name.Equals("Text") && e.NewValue != null)
      {
        int test;
        if(!int.TryParse((string)e.NewValue, out test))
          Text = (string)e.OldValue;
      }
    }
  }
}
