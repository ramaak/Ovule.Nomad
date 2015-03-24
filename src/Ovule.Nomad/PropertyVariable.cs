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
  /// Represents a property.
  /// </summary>
  [Serializable]
  public class PropertyVariable : Variable
  {
    public PropertyVariable(string name, Type type, object value)
      : base(name, type, value)
    {
    }

    public PropertyVariable(string name, string typeFullName, object value)
      : base(name, typeFullName, value)
    {
    }

    public override void CopyFrom(object obj)
    {
      this.ThrowIfArgumentIsNull(() => obj);

      Type objType = obj.GetType();
      CopyFrom(objType, obj);
    }

    public override void CopyFrom(Type type)
    {
      this.ThrowIfArgumentIsNull(() => type);

      CopyFrom(type, null);
    }

    private void CopyFrom(Type type, object obj)
    {
      PropertyInfo property = type.GetProperty(Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
      if (property == null)
        throw new NomadVariableException("Could not find property on type '{0}' matching non-local variable '{1}'", type.FullName, Name);

      Value = property.GetValue(obj, null);
    }

    public override void CopyTo(object obj)
    {
      this.ThrowIfArgumentIsNull(() => obj);

      Type objType = obj.GetType();
      CopyTo(objType, obj);
    }

    public override void CopyTo(Type type)
    {
      CopyTo(type, null);
    }

    private void CopyTo(Type type, object obj)
    {
      PropertyInfo property = type.GetProperty(Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

      if (property == null)
        throw new NomadVariableException("Could not find property on type '{0}' matching '{1}' '{2}'", type.FullName, this.GetType().Name, Name);

      if (property.GetSetMethod(true) == null)
        throw new PropertySetterUnavailableException("Could not find set method on property '{0}' on type '{1}'", Name, type.FullName);

      property.SetValue(obj, Value, null);
    }
  }
}
