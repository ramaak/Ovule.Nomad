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
  /// <summary>
  /// This class generates an assembly that contains everything needed to execute a single method.  The types and methods 
  /// that aren't required are pruned from the assembly that's generated.  Dependant assemblies are bundled up as embedded resources
  /// as this will allow for easier (and more efficient) transfer over the network.
  /// 
  /// At the moment only types and methods are pruned.  Data is avaialble to go further and prune fields/properties however this 
  /// is probably going to cost more (in CPU time) than it's worth (saving on assembly size).  May be considered in future though.
  /// 
  /// It's quite likely in the future that an alternative AssemblyGenerator (or path through this one) will be needed.  This class
  /// might turn out to be pretty inefficient if there are loads of methods that are being executed with Nomad - since an assembly is 
  /// generated per method.  In certain cases it may be better to just transfer everything up front.  The old Nomad Processor can be 
  /// taken out of moth-balls as it's does almost everything we want for ahead-of-time processing.
  /// </summary>
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

    /// <summary>
    /// Generates an assembly that contains 'method' and everything that it depends upon.  The assembly is pruned, removing 
    /// types and methods that are not involved in the execution of the method.
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    public byte[] GenerateAssemblyForMethod(MethodInfo method)
    {
      //TODO: may be worth offering choice to do no pruning, if there are loads of methods that are going to be executed remotely 
      //then it might be more efficient to just transfer everything at the start and not pass lots of pruned (partial) assemblies.
      //Probably the best option is to allow users to pre-process their applications (using a modified version of the old Processor) and
      //this will do this heavy lifting up front rather than at application runtime.
      AssemblyDefinition methAsmDef = GetMethodAssemblyDefinition(method);
      NomadModuleInfo moduleSnapshot = GetRequiredModuleSnapshot(methAsmDef, method);

      CreateEmbeddedResourcesFromReferences(AppDomain.CurrentDomain.BaseDirectory, methAsmDef, moduleSnapshot);
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
        if (typeDef.FullName != "<Module>" && requiredTypes.FirstOrDefault(t => t.FullName == typeDef.FullName) == default(TypeReference))
          assemblyDefToPrune.MainModule.Types.RemoveAt(i);
        else
        {
          if (typeDef.HasMethods)
          {
            IEnumerable<MethodReference> accessedMethRefs = moduleSnapshot.GetNomadMethods(typeDef.FullName);
            for (int j = typeDef.Methods.Count - 1; j >= 0; j--)
            {
              MethodDefinition methDef = typeDef.Methods[j];
              //shouldn't need the EntryPoint but there's a bug/feature in Cecil that means you can't clear the entry point if the asm def 
              //was read from an image (an error will be generated when saving the asm if the entry point's removed)
              if (!methDef.IsConstructor && methDef != assemblyDefToPrune.MainModule.EntryPoint && accessedMethRefs.FirstOrDefault(m => m.FullName == methDef.FullName) == default(MethodReference))
                typeDef.Methods.RemoveAt(j);
            }
          }
        }
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
      //remove file:// prefix on Windows and file: on *nix (with *nix it's fine to have multiple /'s and we need at least one at start) 
      string asmPath = Path.Combine(Path.GetDirectoryName(asmCodeBase), Path.GetFileName(asmCodeBase)).Replace("file:\\", "").Replace("file:", "");

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
    /// <param name="moduleSnapshot"></param>
    /// <param name="knownDependencies"></param>
    /// <returns></returns>
    private IEnumerable<AssemblyDefinition> GetDependencies(string clientAssemblyDir, NomadModuleInfo moduleSnapshot, IDictionary<string, AssemblyDefinition> knownDependencies = null)
    {
      if (knownDependencies == null)
        knownDependencies = new Dictionary<string, AssemblyDefinition>();

      foreach (AssemblyNameReference referencedAssembly in moduleSnapshot.GetDependencies())
      {
        if (!knownDependencies.ContainsKey(referencedAssembly.FullName))
        {
          //ignore assemblies that are part of the .Net framework, these will already be available on server.
          //TODO: Find a better way - at least move out into config so no need to recompile when new libs come along
          //TODO: At the minute just looking in app dir, need to probe like CLR binder
          if (!(referencedAssembly.IsWindowsRuntime || referencedAssembly.Name.ToLower() == "mscorlib" ||
            referencedAssembly.Name.ToLower() == "system" || referencedAssembly.Name.ToLower().StartsWith("system.") ||
            referencedAssembly.Name.ToLower() == "presentationframework" || referencedAssembly.Name.ToLower() == "presentationcore" ||
            referencedAssembly.Name.ToLower() == "windowsbase"))
          {
            string dependentAsmPath = AssemblyUtils.GetAssemblyFilename(clientAssemblyDir, referencedAssembly.Name, true);
            AssemblyDefinition dependentAsmDef = AssemblyDefinition.ReadAssembly(dependentAsmPath);
            PruneAssemblyDefinition(dependentAsmDef, moduleSnapshot);

            knownDependencies.Add(dependentAsmDef.FullName, dependentAsmDef);
            //this will just fill up knownDependencies so don't care about result
            GetDependencies(clientAssemblyDir, moduleSnapshot, knownDependencies);
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
    private void CreateEmbeddedResourcesFromReferences(string clientAssemblyDir, AssemblyDefinition serverAssemblyDef, NomadModuleInfo moduleSnapshot)
    {
      IEnumerable<AssemblyDefinition> dependencies = GetDependencies(clientAssemblyDir, moduleSnapshot);
      if (dependencies != null)
      {
        foreach (AssemblyDefinition dependency in dependencies)
        {
          //TODO: There may be multiple modules in the assembly (fairly low priority)
          serverAssemblyDef.MainModule.Resources.Add(CreateResourceFromAssembly(dependency));
        }
      }
    }

    /// <summary>
    /// Creates an EmbeddedResource to contain an assembly.  Embedding dependencies within the main assembly will make it easier 
    /// and more efficient to transfer over the network.
    /// </summary>
    /// <param name="assemblyDef"></param>
    /// <returns></returns>
    private EmbeddedResource CreateResourceFromAssembly(AssemblyDefinition assemblyDef)
    {
      using (MemoryStream memStream = new MemoryStream())
      {
        assemblyDef.Write(memStream);
        if (memStream == null || memStream.Length == 0)
          throw new FileLoadException(string.Format("Failed to write assembly '{0}' to stream for resource", assemblyDef.FullName));
        return new EmbeddedResource(string.Format("{0}{1}", AssemblyEmbeddedAsResourcePrefix, assemblyDef.FullName), ManifestResourceAttributes.Public, memStream.ToArray());
      }
    }

    #endregion Dependencies
  }
}
