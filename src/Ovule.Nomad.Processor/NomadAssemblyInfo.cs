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
  public class NomadAssemblyInfo
  {
    #region Properties/Fields

    public bool IsDefinedAsNomadAssembly { get; private set; }
    public AssemblyDefinition Assembly { get; private set; }
    public IList<NomadModuleInfo> AccessedModules { get; private set; }

    #endregion Properties/Fields

    #region ctors

    public NomadAssemblyInfo(AssemblyDefinition assemblyDef)
    {
      this.ThrowIfArgumentIsNull(() => assemblyDef);

      Assembly = assemblyDef;
      AccessedModules = new List<NomadModuleInfo>();
      
      IsDefinedAsNomadAssembly = assemblyDef.HasCustomAttributes && assemblyDef.CustomAttributes.FirstOrDefault(
        a => a.AttributeType.FullName == typeof(NomadAssemblyAttribute).FullName) != default(CustomAttribute);

    }

    #endregion ctors

    #region Methods

    public void AddAccessedModule(NomadModuleInfo nomadModuleInfo)
    {
      this.ThrowIfArgumentIsNull(() => nomadModuleInfo);
      if (AccessedModules.FirstOrDefault(m => m.Module.FullyQualifiedName == nomadModuleInfo.Module.FullyQualifiedName) == default(NomadModuleInfo))
        AccessedModules.Add(nomadModuleInfo);
    }

    public bool IsNomadicModule(string moduleFullName)
    {
      this.ThrowIfArgumentIsNoValueString(() => moduleFullName);
      return AccessedModules.FirstOrDefault(m => m.Module.FullyQualifiedName == moduleFullName) != default(NomadModuleInfo);
    }

    #endregion Methods
  }
}
