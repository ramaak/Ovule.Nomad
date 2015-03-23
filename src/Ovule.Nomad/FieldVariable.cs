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
using System.Reflection;

namespace Ovule.Nomad
{
  /// <summary>
  /// Represents a field
  /// </summary>
  [Serializable]
  public class FieldVariable : Variable
  {
    public FieldVariable(string name, Type type, object value)
      : base(name, type, value)
    {
    }

    public FieldVariable(string name, string typeFullName, object value)
      : base(name, typeFullName, value)
    {
    }

    public override void CopyFrom(object obj)
    {
      this.ThrowIfArgumentIsNull(() => obj);

      Type objType = obj.GetType();
      FieldInfo field = objType.GetField(Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
      if (field == null)
        throw new NomadVariableException("Could not find field on type '{0}' matching non-local variable '{1}'", objType.FullName, Name);

      Value = field.GetValue(obj);
    }

    public override void CopyTo(object obj)
    {
      this.ThrowIfArgumentIsNull(() => obj);

      Type objType = obj.GetType();
      FieldInfo field = objType.GetField(Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
      if (field == null)
        throw new NomadVariableException("Could not find field on type '{0}' matching '{1}' '{2}'", objType.FullName, this.GetType().Name, Name);

      field.SetValue(obj, Value);
    }
  }
}
