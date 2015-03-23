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
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Ovule.Nomad.Processor
{
  public class NomadMethodInfo
  {
    public NomadMethodType? NomadMethodType { get; private set; }
    public MethodReference Method { get; private set; }
    public IList<NomadMethodInfo> AccessedMethods { get; private set; }
    public IList<FieldReference> AccessedFields { get; private set; }
    public IList<PropertyReference> AccessedProperties { get; private set; }

    public NomadMethodInfo(NomadMethodType? nomadMethodType, MethodReference methDef)
    {
      this.ThrowIfArgumentIsNull(() => methDef);

      NomadMethodType = nomadMethodType;
      Method = methDef;
      AccessedMethods = new List<NomadMethodInfo>();
      AccessedFields = new List<FieldReference>();
      AccessedProperties = new List<PropertyReference>();
    }

    public bool IsMethodAccessed(string methodFullName)
    {
      return AccessedMethods.FirstOrDefault(m => m.Method.FullName == methodFullName) != default(NomadMethodInfo);
    }

    public void AddAccessedMethod(NomadMethodInfo accessedMethodInfo)
    {
      this.ThrowIfArgumentIsNull(() => accessedMethodInfo);
      if (AccessedMethods.FirstOrDefault(m => m.Method.FullName == accessedMethodInfo.Method.FullName) == default(NomadMethodInfo))
        AccessedMethods.Add(accessedMethodInfo);
    }

    public void AddAccessedField(FieldReference accessedFieldRef)
    {
      this.ThrowIfArgumentIsNull(() => accessedFieldRef);
      if (AccessedFields.FirstOrDefault(f => f.FullName == accessedFieldRef.FullName) == default(FieldReference))
        AccessedFields.Add(accessedFieldRef);
    }

    public void AddAccessedProperty(PropertyReference accessedPropertyRef)
    {
      this.ThrowIfArgumentIsNull(() => accessedPropertyRef);
      if (AccessedProperties.FirstOrDefault(p => p.FullName == accessedPropertyRef.FullName) == default(PropertyReference))
        AccessedProperties.Add(accessedPropertyRef);
    }
  }
}
