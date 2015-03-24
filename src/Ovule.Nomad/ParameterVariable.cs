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

namespace Ovule.Nomad
{
  /// <summary>
  /// Represents a parameter
  /// 
  /// This class doesn't currently add anything to Variable (other than being of a different type)
  /// however this will likely change over time.
  /// </summary>
  [Serializable]
  public class ParameterVariable: Variable
  {
    public ParameterVariable(string name, Type type, object value)
      : base(name, type, value)
    {
    }

    public ParameterVariable(string name, string typeFullName, object value)
      : base(name, typeFullName, value)
    {
    }

    public override void CopyFrom(object obj)
    {
      throw new InvalidOperationException("Cannot copy parameter variables");
    }

    public override void CopyTo(object obj)
    {
      throw new InvalidOperationException("Cannot copy parameter variables");
    }

    public override void CopyFrom(Type type)
    {
      throw new InvalidOperationException("Cannot copy parameter variables");
    }

    public override void CopyTo(Type type)
    {
      throw new InvalidOperationException("Cannot copy parameter variables");
    }
  }
}
