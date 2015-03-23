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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ovule.Nomad.Processor
{
  /// <summary>
  /// Processes an application looking for nomadic methods.  If it finds any it injects calls into these methods
  /// to an INomadClient of type 'nomadClientType'.  A copy of the assembly is also created for use on a Nomad server.
  /// 
  /// Any external dependencies on any processed assembly may not be available to the server.
  /// Rather than copy lots of libraries around all dependencies are saved into the server assembly as embeded 
  /// resources, which the server know how to extract when needed.  In addition to keeping the number of files 
  /// copied low an added benefit of doing this is that it will simplify things when we get to the point where 
  /// nomadic code can be sent to the server dynamically (as opposed to expecting the server to have a static copy 
  /// of the nomadic code) - obviously security is a concern which has to be planned for before doing this.
  /// 
  /// TODO: The server only needs to know about code it can possibly use which means there's the opportunity to 
  /// strip the server assembly right back, extracting all types, methods, fields, etc. it doesn't need and so saving
  /// on file size (and transfer size when thinking about dynamic nomadic code).  Currently this pruning isn't being 
  /// done however most of the information needed to acheive this should be accessible through NomadAssemblyInfo, etc.
  /// </summary>
  public class ApplicationProcessor : IApplicationProcessor
  {
    #region Events

    public event EventHandler<AssemblyProcessedEventArgs> AssemblyProcessed;

    #endregion Events

    #region Properties/Fields

    private const string AssemblyEmbeddedAsResourcePrefix = "NomadRefRes:";

    #endregion Properties/Fields

    #region IApplicationProcessor

    /// <summary>
    /// 
    /// </summary>
    /// <param name="clientType"></param>
    /// <param name="clientApplicationDirectory"></param>
    /// <param name="serverApplicationDirectory"></param>
    public void Process(Type clientType, string clientApplicationDirectory, string serverApplicationDirectory)
    {
      this.ThrowIfArgumentIsNull(() => clientType);
      this.ThrowIfArgumentIsNoValueString(() => clientApplicationDirectory);
      this.ThrowIfArgumentIsNoValueString(() => serverApplicationDirectory);

      AssemblyProcessor asmProcessor = new AssemblyProcessor();

      //Cecil doesn't appear threadsafe at the point of looping over types, not seen any problems with the Parallel.ForEach in this method
      //but I'm not yet processing loads of assemblies.  Playing it safe for now and sticking to 1 thread
      //Parallel.ForEach<string>(Directory.EnumerateFiles(applicationRootDirectory, "*", SearchOption.TopDirectoryOnly), (filename) =>
      foreach (string filePath in Directory.EnumerateFiles(clientApplicationDirectory, "*", SearchOption.TopDirectoryOnly))
      {
        if (filePath.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase) || filePath.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase))
        {
          string asmProcessingError = null;
          bool asmHasNomadicElements = false;
          try
          {
            AssemblyDefinition clientAsmDef = AssemblyDefinition.ReadAssembly(filePath);
            if (clientAsmDef == null)
              throw new NomadProcessorException("Failed to read '{0}' for '{1}", typeof(AssemblyDefinition).Name, filePath);

            NomadAssemblyInfo nomadAssemblyInfo = asmProcessor.Process(clientAsmDef, clientType);
            if (nomadAssemblyInfo != null)
            {
              asmHasNomadicElements = true;
              string serverAsmPath = Path.Combine(serverApplicationDirectory, Path.GetFileName(filePath));
              CreateServerAssembly(nomadAssemblyInfo, filePath, serverAsmPath);
              UpdateClientAssembly(filePath, nomadAssemblyInfo.Assembly);
            }
            Console.WriteLine("{0} '{1}'", asmHasNomadicElements ? "+" : "-", Path.GetFileName(filePath));
          }
          catch (Exception ex)
          {
            Console.WriteLine("Failed to process '{0}': {1}", Path.GetFileName(filePath), ex.Message);
            asmProcessingError = ex.Message;
          }

          if (AssemblyProcessed != null)
            AssemblyProcessed(this, new AssemblyProcessedEventArgs(Path.GetFileName(filePath), asmHasNomadicElements, asmProcessingError));
        }
      }
    }

    #endregion IApplicationProcessor

    #region Client Stuff

    /// <summary>
    /// Save the client assembly with modifications so it talks to a Nomad server
    /// </summary>
    /// <param name="clientAssemblyPath"></param>
    /// <param name="clientAssemblyDef"></param>
    private void UpdateClientAssembly(string clientAssemblyPath, AssemblyDefinition clientAssemblyDef)
    {
      EnsureNomadReferencesAvailableToClient(clientAssemblyDef, Path.GetDirectoryName(clientAssemblyPath));
      clientAssemblyDef.Write(clientAssemblyPath);
    }

    /// <summary>
    /// The client application probably won't reference any Nomad libaries other than Ovule.Nomad (which the [NomadMethod] attribute is in.
    /// After being processed it will now depend on other libs too like Ovule.Nomad.Client.  This project (i.e. the Ovule.Nomad.Processor) 
    /// references everything that's needed so copy any references that are needed from this applications working directory to the client dir.
    /// </summary>
    /// <param name="clientAssemblyDef"></param>
    /// <param name="clientDirectory"></param>
    /// <param name="copiedRefs"></param>
    private void EnsureNomadReferencesAvailableToClient(AssemblyDefinition clientAssemblyDef, string clientDirectory, IList<string> copiedRefs = null)
    {
      string currDir = AppDomain.CurrentDomain.BaseDirectory;
      if (clientAssemblyDef.MainModule.HasAssemblyReferences)
      {
        foreach (AssemblyNameReference clientAsmRef in clientAssemblyDef.MainModule.AssemblyReferences)
        {
          string processorAsmPath = AssemblyUtils.GetAssemblyFilename(currDir, clientAsmRef, false);
          if (!string.IsNullOrWhiteSpace(processorAsmPath))
          {
            if (copiedRefs == null || !copiedRefs.Contains(processorAsmPath))
            {
              File.Copy(processorAsmPath, Path.Combine(clientDirectory, Path.GetFileName(processorAsmPath)), true);
              if (copiedRefs == null)
                copiedRefs = new List<string>();
              copiedRefs.Add(processorAsmPath);

              AssemblyDefinition refDef = AssemblyDefinition.ReadAssembly(processorAsmPath);
              EnsureNomadReferencesAvailableToClient(refDef, clientDirectory, copiedRefs);
            }
          }
        }
      }
    }

    #endregion Client Stuff

    #region Server Stuff

    /// <summary>
    /// Must be called before the modified client assembly is saved
    /// </summary>
    /// <param name="clientAssemblyPath"></param>
    /// <param name="serverAssemblyPath"></param>
    private void CreateServerAssembly(NomadAssemblyInfo nomadAssemblyInfo, string clientAssemblyPath, string serverAssemblyPath)
    {
      AssemblyDefinition serverAsmDef = AssemblyDefinition.ReadAssembly(clientAssemblyPath);
      if (serverAsmDef == null)
        throw new NomadProcessorException("Failed to read '{0}' for '{1} when creating server assembly", typeof(AssemblyDefinition).Name, clientAssemblyPath);

      //Prune(serverAsmDef, nomadAssemblyInfo);

      ConfigureRepeatAndRelayMethods(nomadAssemblyInfo, serverAsmDef);
      CreateEmbeddedResourcesFromReferences(Path.GetDirectoryName(clientAssemblyPath), serverAsmDef);
      serverAsmDef.Write(serverAssemblyPath);
    }

    /// <summary>
    /// The assembly that's being created for the server most likely depends on other assemblies which are not part of the 
    /// .Net framework.  We need these on the server but don't really want to copy loads of DLL's over.  Create an embedded 
    /// resources for each dependency and store them within the actual servere assembly.  The Nomad server know's how to extract
    /// and load these when needed.
    /// 
    /// By having only a single file here it'll also make things easier down the road when we allow for dynamic execution of 
    /// nomadic code.  Obviously security concerns need to be addressed before this is attempted.
    /// </summary>
    /// <param name="clientAssemblyDir"></param>
    /// <param name="serverAssemblyDef"></param>
    private void CreateEmbeddedResourcesFromReferences(string clientAssemblyDir, AssemblyDefinition serverAssemblyDef)
    {
      foreach (ModuleDefinition modDef in serverAssemblyDef.Modules)
      {
        if (modDef.HasAssemblyReferences)
        {
          foreach (AssemblyNameReference asmNameRef in modDef.AssemblyReferences.ToArray())
          {
            //ignore assemblies that are part of the .Net framework, these will already be available on server.
            //TODO: Find a better way - at least move out into config so no need to recompile when new libs come along
            //TODO: At the minute just looking in app dir, need to probe like CLR binder
            if (!(asmNameRef.IsWindowsRuntime || asmNameRef.Name.ToLower() == "mscorlib" ||
              asmNameRef.Name.ToLower() == "system" || asmNameRef.Name.ToLower().StartsWith("system.") ||
              asmNameRef.Name.ToLower() == "presentationframework" || asmNameRef.Name.ToLower() == "presentationcore" ||
              asmNameRef.Name.ToLower() == "windowsbase"))
              modDef.Resources.Add(CreateResourceFromAssembly(AssemblyUtils.GetAssemblyFilename(clientAssemblyDir, asmNameRef.Name)));
          }
        }
      }
    }

    /// <summary>
    /// Creates an EmbeddedResource to contain file assembly at path 'assemblyPath'
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="assemblyName"></param>
    /// <returns></returns>
    private EmbeddedResource CreateResourceFromAssembly(string assemblyPath)
    {
      AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(assemblyPath);
      byte[] asmBytes = File.ReadAllBytes(assemblyPath);
      if (asmBytes == null || asmBytes.Length == 0)
        throw new FileLoadException(string.Format("Failed to read assembly file '{0}'", assemblyPath));
      return new EmbeddedResource(string.Format("{0}{1}", AssemblyEmbeddedAsResourcePrefix, asmDef.FullName), ManifestResourceAttributes.Public, asmBytes);
    }

    private void ConfigureRepeatAndRelayMethods(NomadAssemblyInfo assemblyInfo, AssemblyDefinition serverAssemblyDef)
    {
      if (assemblyInfo.AccessedModules != null)
      {
        foreach (NomadModuleInfo moduleInfo in assemblyInfo.AccessedModules)
        {
          if (moduleInfo.AccessedTypes != null)
          {
            foreach (NomadTypeInfo typeInfo in moduleInfo.AccessedTypes)
            {
              if (typeInfo.AccessedMethods != null)
              {
                foreach (NomadMethodInfo methodInfo in typeInfo.AccessedMethods)
                {
                  NomadMethodType nomMethType = methodInfo.NomadMethodType.GetValueOrDefault(NomadMethodType.Normal);
                  if (nomMethType == NomadMethodType.Relay || nomMethType == NomadMethodType.Repeat)
                  {
                    bool foundServerMethDef = false;
                    ModuleDefinition serverModDef = serverAssemblyDef.Modules.FirstOrDefault(m => m.Name == moduleInfo.Module.Name);
                    if(serverModDef != default(ModuleDefinition))
                    {
                      TypeDefinition serverTypeDef = serverModDef.Types.FirstOrDefault(t => t.FullName == typeInfo.Type.FullName);
                      if(serverTypeDef != default(TypeDefinition))
                      {
                        MethodDefinition serverMethDef = serverTypeDef.Methods.FirstOrDefault(m => m.FullName == methodInfo.Method.FullName);
                        if(serverMethDef != default(MethodDefinition))
                        {
                          foundServerMethDef = true;
                          if (nomMethType == NomadMethodType.Relay)
                            new RelayMethodProcessor(false).InjectIsRelayCallParameter(serverMethDef);
                          else
                            new RepeatMethodProcessor(false).InjectIsRepeatCallParameter(serverMethDef);
                        }
                      }
                    }
                    if (!foundServerMethDef)
                      throw new NomadProcessorException("Failed to resolve server method '{0}'", methodInfo.Method.FullName);
                  }
                }
              }
            }
          }
        }
      }
    }

    #endregion Server Stuff
  }
}