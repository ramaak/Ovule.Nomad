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
using System.Windows.Input;

namespace Ovule.Nomad.Processor.Gui
{
  public class RelayCommand : ICommand
  {
    #region Properties/Fields

    private Action _execute;
    private Func<bool> _canExecute;

    #endregion Properties/Fields

    #region ctors

    public RelayCommand(Action execute, Func<bool> canExecute)
    {
      this._execute = execute;
      this._canExecute = canExecute;
    }

    public RelayCommand(Action execute)
      : this(execute, null)
    {
    }

    #endregion ctors

    #region ICommand

    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object parameter)
    {
      if (this._canExecute == null)
        return true;
      else
      {
        bool result = this._canExecute.Invoke();
        return result;
      }
    }

    public void Execute(object parameter)
    {
      this._execute.Invoke();
    }

    #endregion ICommand
  }
}
