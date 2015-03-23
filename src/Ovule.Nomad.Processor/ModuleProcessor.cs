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
using System.IO;
using System.Linq;

namespace Ovule.Nomad.Processor
{
  public class ModuleProcessor : IModuleProcessor
  {
    #region IModuleProcessor

    /// <summary>
    /// Processes all nomadic elements of a module
    /// </summary>
    /// <param name="moduleDef"></param>
    /// <param name="nomadClientType"></param>
    /// <param name="assemblyDef"></param>
    /// <returns></returns>
    public NomadModuleInfo Process(ModuleDefinition moduleDef, Type nomadClientType, AssemblyDefinition assemblyDef)
    {
      NomadModuleInfo nomadModuleInfo = null;
      if (moduleDef.HasTypes)
      {
        if (moduleDef.AssemblyResolver is BaseAssemblyResolver)
          ((BaseAssemblyResolver)moduleDef.AssemblyResolver).AddSearchDirectory(Path.GetDirectoryName(assemblyDef.MainModule.FullyQualifiedName));

        TypeProcessor typeProcessor = new TypeProcessor();
        foreach (TypeDefinition typeDef in moduleDef.Types)
        {
          if (typeDef.Name != "<Module>")
          {
            if (typeDef.HasMethods)
            {
              NomadTypeInfo nomadTypeInfo = typeProcessor.Process(typeDef, nomadClientType);
              if (nomadTypeInfo != null && nomadTypeInfo.AccessedMethods != null && nomadTypeInfo.AccessedMethods.Count > 0)
              {
                if (nomadModuleInfo == null)
                  nomadModuleInfo = new NomadModuleInfo(moduleDef);
                nomadModuleInfo.AddAccessedType(nomadTypeInfo);
              }
            }
          }
        }
      }
      return nomadModuleInfo;
    }

    #endregion IModuleProcessor
  }
}
