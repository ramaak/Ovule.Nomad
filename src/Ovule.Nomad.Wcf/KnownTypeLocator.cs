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
using Ovule.Diagnostics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ovule.Nomad.Wcf
{
  /// <summary>
  /// The KnownTypeLocator searches for assemblies that contain types implementing IWcfKnownTypeProvider and when it finds these types
  /// calls IWcfKnownTypeProvider.GetKnownTypes() to get the collection of types that are to be known to the WCF DataContractSerializer. 
  /// By default this class will probe the base directory for the current AppDomain and also a directory called 'bin' off this if it exists. 
  /// If you would like to override the default probing directories then add an entry to 'appSettings' in the applications configuration file 
  /// called 'KnownTypeAssemblyProbingPaths' and specify the list of probing paths as a CSV string.
  /// </summary>
  public static class KnownTypeLocator
  {
    #region Properties/Fields

    private static object _getKnownTypesLock = new object();

    private const string KnownTypeAssemblyProbingPathsConfig = "KnownTypeAssemblyProbingPaths";

    private static ILogger _logger = LoggerFactory.Create(typeof(KnownTypeLocator).FullName);
    private static IList<Type> _knownTypes = null;
    private static IEnumerable<string> _knownTypeAssemblyProbingPaths;

    public static IEnumerable<Type> KnownTypes { get { return GetKnownTypes(null); } }

    #endregion Properties/Fields

    #region ctors

    static KnownTypeLocator()
    {
      string overridenKnownTypeAssemblyProbingPaths = ConfigurationManager.AppSettings[KnownTypeAssemblyProbingPathsConfig];
      List<string> probePaths = new List<string>();
      if (string.IsNullOrWhiteSpace(overridenKnownTypeAssemblyProbingPaths))
      {
        probePaths.Add(AppDomain.CurrentDomain.BaseDirectory);
        _logger.LogInfo("Adding probe path '{0}'", AppDomain.CurrentDomain.BaseDirectory);
        string binProbePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
        if (Directory.Exists(binProbePath))
        {
          _logger.LogInfo("Adding probe path '{0}'", binProbePath);
          probePaths.Add(binProbePath);
        }
      }
      else
      {
        string[] overridenPaths = overridenKnownTypeAssemblyProbingPaths.Split(',');
        foreach (string overridenPath in overridenPaths)
        {
          string path = overridenPath.Trim();
          if (!Directory.Exists(path))
            _logger.LogError("The probing path '{0}' specified in the applications configuration setting '{1}' does not exist", path, KnownTypeAssemblyProbingPathsConfig);
          else
          {
            probePaths.Add(path);
            _logger.LogInfo("Adding probe path '{0}'", path);
          }
        }
      }
      if (probePaths == null || !probePaths.Any())
        _logger.LogError("No probing paths defined.");
      _knownTypeAssemblyProbingPaths = probePaths;
    }

    #endregion ctors

    #region Methods

    /// <summary>
    /// Returns all Types reported by all implementation of IWcfKnownTypeProvider found in assemblies on the probing paths.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
    {
      if (_knownTypes == null)
      {
        lock (_getKnownTypesLock)
        {
          if (_knownTypes == null)
          {
            _knownTypes = new List<Type>();
            RecordKnownTypes(GetKnownTypesFromProviders());
          }
          if (!_knownTypes.Any())
            _logger.LogWarning("GetKnownTypes: No known types were found in assemblies on the known type probing path.  This may be a problem.");
        }
      }
      return _knownTypes;
    }

    private static void RecordKnownTypes(IEnumerable<Type> typesToRecord)
    {
      if (typesToRecord != null && typesToRecord.Any())
      {
        foreach (Type typeToRecord in typesToRecord)
        {
          if (!_knownTypes.Contains(typeToRecord))
          {
            _knownTypes.Add(typeToRecord);
            _logger.LogInfo("RecordKnownTypes: New known type: '{0}'", typeToRecord.FullName);
          }
        }
      }
    }

    private static IEnumerable<Type> GetKnownTypesFromProviders()
    {
      List<Type> knownTypes = new List<Type>();

      IEnumerable<Type> knownTypeProviderTypes = GetKnownTypeProviderTypes();
      if (knownTypeProviderTypes == null || !knownTypeProviderTypes.Any())
        _logger.LogWarning("GetKnownTypesFromProviders: No implementations of '{0}' where found in assemblies on the known type probing path. This may be a problem", typeof(IWcfKnownTypeProvider));
      else
      {
        foreach (Type knownTypeProviderType in knownTypeProviderTypes)
        {
          IWcfKnownTypeProvider knownTypeProvider = (IWcfKnownTypeProvider)Activator.CreateInstance(knownTypeProviderType);
          IEnumerable<Type> providerKnownTypes = knownTypeProvider.GetKnownTypes();
          if (providerKnownTypes == null || !providerKnownTypes.Any())
            _logger.LogWarning("GetKnownTypesFromProviders: An instance of '{0}' did not return any known types", knownTypeProviderType.FullName);
          else
            knownTypes.AddRange(providerKnownTypes);
        }
      }
      return knownTypes;
    }

    /// <summary>
    /// Returns all Types contained within assemblies on the probing paths that implement IWcfKnownTypeProvider.
    /// </summary>
    /// <returns></returns>
    private static IEnumerable<Type> GetKnownTypeProviderTypes()
    {
      List<Type> knownTypeProviders = new List<Type>();
      //the required assemblies may not yet be in the process address space, work with files.
      foreach (string probePath in _knownTypeAssemblyProbingPaths)
      {
        _logger.LogInfo("GetKnownTypeProviderTypes: Searching for known type Assemblies in '{0}'", probePath);
        if (Directory.Exists(probePath))
        {
          foreach (string file in Directory.EnumerateFiles(probePath))
          {
            if (Path.HasExtension(file))
            {
              string ext = Path.GetExtension(file).ToLower();
              if (ext == ".exe" || ext == ".dll")
              {
                try
                {
                  _logger.LogInfo("GetKnownTypeProviderTypes: Inspecting assembly '{0}' for types implementing '{1}'", file, typeof(IWcfKnownTypeProvider).FullName);
                  Assembly asm = Assembly.LoadFrom(file);
                  Type[] types = asm.GetTypes();
                  if (types != null && types.Length > 0)
                  {
                    foreach (Type type in types)
                    {
                      if (typeof(IWcfKnownTypeProvider).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                        knownTypeProviders.Add(type);
                    }
                  }
                }
                catch (BadImageFormatException bifEx)
                {
                  //likely the file just isn't a .Net assembly
                  _logger.LogWarning("GetKnownTypeProviderTypes: File '{0}' could not be inspected while discovering known types because it's not a valid .Net assembly [{1}]", file, bifEx.Message);
                }
                catch (Exception ex)
                {
                  string message = string.Format("File '{0}' could not be inspected while discovering known types.", file);
                  _logger.LogException(ex, "GetKnownTypeProviderTypes: " + message);
                  throw new NomadException(string.Format("{0}. Please consult the Nomad log.", ex.Message), ex);
                }
              }
            }
          }
        }
      }
      return knownTypeProviders;
    }

    #endregion Methods
  }
}
