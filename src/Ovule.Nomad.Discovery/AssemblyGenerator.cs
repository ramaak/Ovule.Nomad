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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ovule.Nomad.Discovery
{
  public class AssemblyGenerator
  {
    #region Properties/Fields

    private const string AssemblyEmbeddedAsResourcePrefix = "NomadRefRes:";

    #endregion Properties/Fields

    #region ctors

    public AssemblyGenerator()
    {
    }

    #endregion ctors

    #region General Methods

    public byte[] GenerateAssemblyForMethod(MethodInfo method)
    {
      AssemblyDefinition methAsmDef = GetMethodAssemblyDefinition(method);
      NomadModuleInfo moduleSnapshot = GetRequiredModuleSnapshot(methAsmDef, method);

      CreateEmbeddedResourcesFromReferences(AppDomain.CurrentDomain.BaseDirectory, methAsmDef);
      //PruneAssemblyDefinition(methAsmDef, moduleSnapshot);
      using(MemoryStream rawAsmStream = new MemoryStream())
      {
        methAsmDef.Write(rawAsmStream);
        return rawAsmStream.ToArray();
      }
    }

    private void PruneAssemblyDefinition(AssemblyDefinition assemblyDefToPrune, NomadModuleInfo moduleSnapshot)
    {
      if (assemblyDefToPrune.Modules.Count != 1)
        throw new NotSupportedException("Currently only assemblies with a single module are supported");

      if (!assemblyDefToPrune.MainModule.HasTypes)
        throw new InvalidOperationException(string.Format("The main in assembly '{0}' contains to types", assemblyDefToPrune.FullName));

      IEnumerable<TypeReference> requiredTypes = moduleSnapshot.GetNomadTypes();
      for (int i = assemblyDefToPrune.MainModule.Types.Count - 1; i >= 0; i--) //deleting items so move backwards
      {
        TypeDefinition typeDef = assemblyDefToPrune.MainModule.Types[i];
        if (requiredTypes.FirstOrDefault(t => t.FullName == typeDef.FullName) == default(TypeReference))
          assemblyDefToPrune.MainModule.Types.RemoveAt(i);
      }
    }

    private NomadModuleInfo GetRequiredModuleSnapshot(AssemblyDefinition assemblyDef, MethodInfo method)
    {
      NomadMethodInfo nomMethInfo = new MethodDiscoverer(assemblyDef, method).Discover();

      NomadTypeInfo nomTypeInfo = new NomadTypeInfo(nomMethInfo.Method.DeclaringType.Resolve());
      nomTypeInfo.AddAccessedMethod(nomMethInfo);

      NomadModuleInfo nomModInfo = new NomadModuleInfo(nomMethInfo.Method.Module);
      nomModInfo.AddAccessedType(nomTypeInfo);

      return nomModInfo;
    }

    private AssemblyDefinition GetMethodAssemblyDefinition(MethodInfo method)
    {
      string asmCodeBase = method.DeclaringType.Assembly.CodeBase;
      string asmPath = Path.Combine(Path.GetDirectoryName(asmCodeBase), Path.GetFileName(asmCodeBase)).Replace(@"file:\\", "").Replace("file:\\", "").Replace("file://", "").Replace("file:/", "").Replace("file:", "");

      AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(asmPath);
      if (asmDef == null)
        throw new NomadDiscoveryException("Could not read assembly holding method '{0}'", method.Name);
      return asmDef;
    }

    #endregion General Methods

    #region Dependencies

    /// <summary>
    /// Return all dependencies of 'assemblyDef'
    /// </summary>
    /// <param name="clientAssemblyDir"></param>
    /// <param name="assemblyDef"></param>
    /// <param name="knownDependencies"></param>
    /// <returns></returns>
    private IEnumerable<AssemblyDefinition> GetDependencies(string clientAssemblyDir, AssemblyDefinition assemblyDef, IDictionary<string, AssemblyDefinition> knownDependencies = null)
    {
      if (knownDependencies == null)
        knownDependencies = new Dictionary<string, AssemblyDefinition>();
      foreach (ModuleDefinition modDef in assemblyDef.Modules)
      {
        if (modDef.HasAssemblyReferences)
        {
          foreach (AssemblyNameReference asmNameRef in modDef.AssemblyReferences.ToArray())
          {
            if (!knownDependencies.ContainsKey(asmNameRef.FullName))
            {
              //ignore assemblies that are part of the .Net framework, these will already be available on server.
              //TODO: Find a better way - at least move out into config so no need to recompile when new libs come along
              //TODO: At the minute just looking in app dir, need to probe like CLR binder
              if (!(asmNameRef.IsWindowsRuntime || asmNameRef.Name.ToLower() == "mscorlib" ||
                asmNameRef.Name.ToLower() == "system" || asmNameRef.Name.ToLower().StartsWith("system.") ||
                asmNameRef.Name.ToLower() == "presentationframework" || asmNameRef.Name.ToLower() == "presentationcore" ||
                asmNameRef.Name.ToLower() == "windowsbase"))
              {
                string dependentAsmPath = AssemblyUtils.GetAssemblyFilename(clientAssemblyDir, asmNameRef.Name, true);
                AssemblyDefinition dependentAsmDef = AssemblyDefinition.ReadAssembly(dependentAsmPath);

                knownDependencies.Add(dependentAsmDef.FullName, dependentAsmDef);
                //this will just fill up knownDependencies so don't care about result
                GetDependencies(clientAssemblyDir, dependentAsmDef, knownDependencies);
              }
            }
          }
        }
      }
      return knownDependencies.Values;
    }

    /// <summary>
    /// The assembly that's being created for the server most likely depends on other assemblies which are not part of the 
    /// .Net framework.  We need these on the server but don't really want to copy loads of DLL's over.  Create an embedded 
    /// resources for each dependency and store them within the actual server assembly.  The Nomad server know's how to extract
    /// and load these when needed.
    /// 
    /// Pruning isn't done yet, as a result this method will inflate the assmebly (potentially dramatically).
    /// </summary>
    /// <param name="clientAssemblyDir"></param>
    /// <param name="serverAssemblyDef"></param>
    private void CreateEmbeddedResourcesFromReferences(string clientAssemblyDir, AssemblyDefinition serverAssemblyDef)
    {
      IEnumerable<AssemblyDefinition> dependencies = GetDependencies(clientAssemblyDir, serverAssemblyDef);
      if (dependencies.Any())
      {
        foreach (AssemblyDefinition dependency in dependencies)
        {
          string dependentAsmPath = AssemblyUtils.GetAssemblyFilename(clientAssemblyDir, dependency.Name, true);
          AssemblyDefinition dependentAsmDef = AssemblyDefinition.ReadAssembly(dependentAsmPath);
          //TODO: There may be multiple modules in the assembly
          serverAssemblyDef.MainModule.Resources.Add(CreateResourceFromAssembly(dependentAsmPath, dependentAsmDef));
        }
      }
    }

    /// <summary>
    /// Creates an EmbeddedResource to contain assembly on path 'assemblyPath'
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="assemblyPath"></param>
    /// <param name="assemblyDef"></param>
    /// <returns></returns>
    private EmbeddedResource CreateResourceFromAssembly(string assemblyPath, AssemblyDefinition assemblyDef)
    {
      byte[] asmBytes = File.ReadAllBytes(assemblyPath);
      if (asmBytes == null || asmBytes.Length == 0)
        throw new FileLoadException(string.Format("Failed to read assembly file '{0}'", assemblyPath));
      return new EmbeddedResource(string.Format("{0}{1}", AssemblyEmbeddedAsResourcePrefix, assemblyDef.FullName), ManifestResourceAttributes.Public, asmBytes);
    }

    #endregion Dependencies
  }
}
