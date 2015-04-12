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

namespace Ovule.Nomad.Discovery
{
  public class NomadTypeInfo
  {
    public bool IsDefinedAsNomadType { get; private set; }
    public TypeDefinition Type { get; private set; }
    public IList<NomadMethodInfo> AccessedMethods { get; private set; }

    public NomadTypeInfo(TypeDefinition typeDef)
    {
      this.ThrowIfArgumentIsNull(() => typeDef);
      
      Type = typeDef;
      AccessedMethods = new List<NomadMethodInfo>();

      IsDefinedAsNomadType = typeDef.HasCustomAttributes && 
        typeDef.CustomAttributes.FirstOrDefault(t => t.AttributeType.FullName == typeof(NomadTypeAttribute).FullName) != default(CustomAttribute);
    }

    public void AddAccessedMethod(NomadMethodInfo nomadMethodInfo)
    {
      this.ThrowIfArgumentIsNull(() => nomadMethodInfo);
      if (AccessedMethods.FirstOrDefault(m => m.Method.FullName == nomadMethodInfo.Method.FullName) == default(NomadMethodInfo))
        AccessedMethods.Add(nomadMethodInfo);
    }
  }
}
