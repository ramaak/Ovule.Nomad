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
  public class NomadModuleInfo
  {
    #region Properties/Fields

    private bool _needsFlattened;
    private IDictionary<string, TypeReference> _nomadTypes;
    private IDictionary<string, IList<MethodReference>> _nomadMethods;
    private IDictionary<string, IList<FieldReference>> _nomadFields;
    private IDictionary<string, IList<PropertyReference>> _nomadProperties;
    private IDictionary<string, AssemblyNameReference> _nomadDependentModules;

    public ModuleDefinition Module { get; private set; }
    public IList<NomadTypeInfo> AccessedTypes { get; private set; }

    #endregion Properties/Fields

    #region ctors

    public NomadModuleInfo(ModuleDefinition moduleDef)
    {
      this.ThrowIfArgumentIsNull(() => moduleDef);

      Module = moduleDef;
      AccessedTypes = new List<NomadTypeInfo>();
    }

    #endregion ctors

    #region Public

    public void AddAccessedType(NomadTypeInfo nomadTypeInfo)
    {
      this.ThrowIfArgumentIsNull(() => nomadTypeInfo);
      if (AccessedTypes.FirstOrDefault(t => t.Type.FullName == nomadTypeInfo.Type.FullName) == default(NomadTypeInfo))
      {
        AccessedTypes.Add(nomadTypeInfo);
        _needsFlattened = true;
      }
    }

    public IEnumerable<TypeReference> GetNomadTypes()
    {
      ProduceFlattenedStructures();
      return _nomadTypes.Values;
    }

    public IEnumerable<MethodReference> GetNomadMethods(string forTypeFullName)
    {
      ProduceFlattenedStructures();
      if (_nomadMethods.ContainsKey(forTypeFullName))
        return _nomadMethods[forTypeFullName];
      return null;
    }

    public IEnumerable<FieldReference> GetNomadFields(string forTypeFullName)
    {
      ProduceFlattenedStructures();
      if (_nomadFields.ContainsKey(forTypeFullName))
        return _nomadFields[forTypeFullName];
      return null;
    }

    public IEnumerable<PropertyReference> GetNomadProperties(string forTypeFullName)
    {
      ProduceFlattenedStructures();
      if (_nomadProperties.ContainsKey(forTypeFullName))
        return _nomadProperties[forTypeFullName];
      return null;
    }

    public bool IsTypeNomadic(string typeFullName)
    {
      ProduceFlattenedStructures();
      return _nomadTypes.ContainsKey(typeFullName);
    }

    public bool IsMethodNomadic(string typeFullName, string methodFullName)
    {
      return IsMemberReferenceNomadic(typeFullName, methodFullName, GetNomadMethods(typeFullName));
    }

    public bool IsFieldNomadic(string typeFullName, string fieldFullName)
    {
      return IsMemberReferenceNomadic(typeFullName, fieldFullName, GetNomadFields(typeFullName));
    }

    public bool IsPropertyNomadic(string typeFullName, string propertyFullName)
    {
      return IsMemberReferenceNomadic(typeFullName, propertyFullName, GetNomadProperties(typeFullName));
    }

    public IEnumerable<AssemblyNameReference> GetDependencies()
    {
      ProduceFlattenedStructures();
      if (_nomadDependentModules == null)
        return null;
      return _nomadDependentModules.Values;
    }

    #endregion Public

    #region Util

    private bool IsMemberReferenceNomadic<T>(string typeFullName, string memberFullName, IEnumerable<T> nomadicMembers) where T : MemberReference
    {
      return nomadicMembers != null && nomadicMembers.FirstOrDefault(m => m.FullName == memberFullName) != default(T);
    }

    private void ProduceFlattenedStructures()
    {
      if (_needsFlattened)
      {
        _nomadTypes = new Dictionary<string, TypeReference>();
        _nomadMethods = new Dictionary<string, IList<MethodReference>>();
        _nomadFields = new Dictionary<string, IList<FieldReference>>();
        _nomadProperties = new Dictionary<string, IList<PropertyReference>>();
        _nomadDependentModules = new Dictionary<string, AssemblyNameReference>();

        if (AccessedTypes != null && AccessedTypes.Any())
        {
          foreach (NomadTypeInfo typeInfo in AccessedTypes)
          {
            if (typeInfo.AccessedMethods != null && typeInfo.AccessedMethods.Any())
            {
              foreach (NomadMethodInfo methInfo in typeInfo.AccessedMethods)
                FlattenMethod(methInfo);
            }
          }
        }
        _needsFlattened = false;
      }
    }

    private void CheckSaveFlatMemberReference<T>(T memberRef, IDictionary<string, IList<T>> memberRefRecord) where T : MemberReference
    {
      this.ThrowIfArgumentIsNull(() => memberRef);
      this.ThrowIfArgumentIsNull(() => memberRefRecord);

      if (!_nomadTypes.ContainsKey(memberRef.DeclaringType.FullName))
        _nomadTypes.Add(memberRef.DeclaringType.FullName, memberRef.DeclaringType);

      if (!memberRefRecord.ContainsKey(memberRef.DeclaringType.FullName))
        memberRefRecord.Add(memberRef.DeclaringType.FullName, new List<T>());
      if (memberRefRecord[memberRef.DeclaringType.FullName].FirstOrDefault(mr => mr.FullName == memberRef.FullName) == default(T))
        memberRefRecord[memberRef.DeclaringType.FullName].Add(memberRef);

      if (memberRef.DeclaringType.Scope is AssemblyNameReference)
      {
        string refModName = ((AssemblyNameReference)memberRef.DeclaringType.Scope).FullName;
        if (refModName != null && refModName != Module.Assembly.FullName && !_nomadDependentModules.ContainsKey(refModName))
          _nomadDependentModules.Add(refModName, (AssemblyNameReference)memberRef.DeclaringType.Scope);
      }
    }

    private void FlattenMethod(NomadMethodInfo methInfo)
    {
      this.ThrowIfArgumentIsNull(() => methInfo);

      CheckSaveFlatMemberReference(methInfo.Method, _nomadMethods);
      if (methInfo.Method.ReturnType.FullName != typeof(void).FullName)
      {
        if (!_nomadTypes.ContainsKey(methInfo.Method.ReturnType.FullName))
          _nomadTypes.Add(methInfo.Method.ReturnType.FullName, methInfo.Method.ReturnType);
      }
      if (methInfo.Method.HasParameters)
      {
        foreach (ParameterDefinition paramDef in methInfo.Method.Parameters)
        {
          if (!_nomadTypes.ContainsKey(paramDef.ParameterType.FullName))
            _nomadTypes.Add(paramDef.ParameterType.FullName, paramDef.ParameterType);
        }
      }

      if (methInfo.AccessedMethods != null && methInfo.AccessedMethods.Any())
      {
        foreach (NomadMethodInfo accessedMethInfo in methInfo.AccessedMethods)
          FlattenMethod(accessedMethInfo);
      }
      if (methInfo.AccessedFields != null && methInfo.AccessedFields.Any())
      {
        foreach (FieldReference fieldRef in methInfo.AccessedFields)
          CheckSaveFlatMemberReference(fieldRef, _nomadFields);
      }
      if (methInfo.AccessedProperties != null && methInfo.AccessedProperties.Any())
      {
        foreach (PropertyReference propertyRef in methInfo.AccessedProperties)
          CheckSaveFlatMemberReference(propertyRef, _nomadProperties);
      }
    }

    #endregion Util
  }
}
