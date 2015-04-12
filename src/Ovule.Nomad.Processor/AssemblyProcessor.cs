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
using System;
using System.Linq;

namespace Ovule.Nomad.Processor
{
  public class AssemblyProcessor : IAssemblyProcessor
  {
    #region IAssemblyProcessor

    public NomadAssemblyInfo Process(AssemblyDefinition assemblyDef, Type nomadClientType)
    {
      this.ThrowIfArgumentIsNull(() => assemblyDef);
      this.ThrowIfArgumentIsNull(() => nomadClientType);

      //if (!typeof(INomadClient).IsAssignableFrom(nomadClientType) || nomadClientType.IsInterface || nomadClientType.IsAbstract)
      //  throw new ArgumentException(string.Format("The 'nomadClientType' argument is invalid.  It must be a concrete type that implements '{0}'", typeof(INomadClient)));

      if (assemblyDef.Modules == null || assemblyDef.Modules.Count == 0)
        throw new NomadException(string.Format("Assembly '{0}' contains no modules", assemblyDef.FullName));

      NomadAssemblyInfo nomadAssemblyInfo = new NomadAssemblyInfo(assemblyDef);
      ModuleProcessor moduleProcessor = new ModuleProcessor();
      foreach (ModuleDefinition moduleDef in assemblyDef.Modules)
      {
        NomadModuleInfo moduleInfo = moduleProcessor.Process(moduleDef, nomadAssemblyInfo.IsDefinedAsNomadAssembly, nomadClientType, assemblyDef);
        if (moduleInfo != null)
          nomadAssemblyInfo.AddAccessedModule(moduleInfo);
      }

      if (nomadAssemblyInfo.AccessedModules.Count > 0)
        return nomadAssemblyInfo;
      return null;
    }

    #endregion IAssemblyProcessor
  }
}
