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
using Mono.Cecil.Cil;
using System.Linq;
using System.Reflection;

namespace Ovule.Nomad.Discovery
{
  public class MethodDiscoverer
  {
    #region Properties/Fields

    private MethodInfo _methInfo;
    private AssemblyDefinition _assemblyDef;

    #endregion Properties/Fields

    #region ctors

    public MethodDiscoverer(AssemblyDefinition assemblyDef, MethodInfo method)
    {
      this.ThrowIfArgumentIsNull(() => assemblyDef);
      this.ThrowIfArgumentIsNull(() => method);

      _assemblyDef = assemblyDef;
      _methInfo = method;
    }

    #endregion ctors

    #region Methods

    private MethodDefinition GetMethodDefinition()
    {
      if (_assemblyDef.Modules != null)
      {
        foreach (ModuleDefinition modDef in _assemblyDef.Modules)
        {
          if (modDef.HasTypes)
          {
            TypeDefinition typeDef = modDef.Types.FirstOrDefault(t => t.FullName == _methInfo.DeclaringType.FullName);
            if (typeDef != default(TypeDefinition))
            {
#warning need to account for overloads
              MethodDefinition methDef = typeDef.Methods.FirstOrDefault(m => m.Name == _methInfo.Name);
              if (methDef != default(MethodDefinition))
                return methDef;
              throw new NomadDiscoveryException("Did not find suitable implementation of method '{0}.{1}' in assembly '{2}'", _methInfo.DeclaringType.FullName, _methInfo.Name, _methInfo.DeclaringType.Assembly.FullName);
            }
          }
        }
      }
      throw new NomadDiscoveryException("Did not find suitable implementation of method '{0}.{1}' in assembly '{2}'", _methInfo.DeclaringType.FullName, _methInfo.Name, _methInfo.DeclaringType.Assembly.FullName);
    }

    public NomadMethodInfo Discover()
    {
      MethodDefinition methDef = GetMethodDefinition();
      NomadMethodInfo nomadMethInfo = new NomadMethodInfo(methDef);
      DiscoverNonLocalReferencesAndLinkedMethods(methDef.DeclaringType, methDef, nomadMethInfo);
      return nomadMethInfo;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeDef"></param>
    /// <param name="methodDef"></param>
    /// <param name="nomadMethInfo"></param>
    private void DiscoverNonLocalReferencesAndLinkedMethods(TypeDefinition typeDef, MethodDefinition methodDef, NomadMethodInfo nomadMethInfo)
    {
      this.ThrowIfArgumentIsNull(() => typeDef);
      this.ThrowIfArgumentIsNull(() => methodDef);
      this.ThrowIfArgumentIsNull(() => nomadMethInfo);

      if (typeDef.Namespace == "System" || typeDef.Namespace.StartsWith("System."))
        return;

      if (methodDef.Body == null || methodDef.Body.Instructions == null)
        return;

      foreach (Instruction instruction in methodDef.Body.Instructions)
      {
        //have we got a field reference?
        if (instruction.OpCode.OperandType == OperandType.InlineField)
        {
          FieldReference fieldRef = (FieldReference)instruction.Operand;
          ProcessDiscoveredField(fieldRef, nomadMethInfo);
        }
        //or a method call (this includes property access)
        else if (instruction.OpCode.OperandType == OperandType.InlineMethod)
        {
          MethodReference calledMethDef = (MethodReference)instruction.Operand;

          if (calledMethDef.DeclaringType != null && calledMethDef.DeclaringType.FullName == "Ovule.Nomad.Client.NomadClient")//typeof(NomadClient).FullName)
            return; //multiple methods may reference each other, don't process them a second time
          //don't worry about message as meaningless to user if in recursive call, will get caught further up stack and rethrown
          //throw new MethodAlreadyProcessedException();

          if (calledMethDef != methodDef && !nomadMethInfo.IsMethodAccessed(calledMethDef.FullName))
            ProcessDiscoveredMethod(calledMethDef, typeDef, methodDef, nomadMethInfo);
        }
      }
    }

    private void ProcessDiscoveredField(FieldReference fieldRef, NomadMethodInfo nomadMethodInfo)
    {
      nomadMethodInfo.AddAccessedField(fieldRef);
    }

    private MethodDefinition GetMethodDefinitionFromReference(MethodReference methodRef)
    {
      this.ThrowIfArgumentIsNull(() => methodRef);

      if (methodRef is MethodDefinition)
        return (MethodDefinition)methodRef;
      return methodRef.Resolve();
    }

    private void ProcessDiscoveredMethod(MethodReference calledMethodRef, TypeDefinition typeDef, MethodDefinition methodDef, NomadMethodInfo nomadMethodInfo)
    {
      MethodDefinition calledMethDef = GetMethodDefinitionFromReference(calledMethodRef);
      if (calledMethDef.IsGetter || calledMethDef.IsSetter)
      {
        string propMethName = calledMethodRef.Name;
        if (propMethName.StartsWith("get_") || propMethName.StartsWith("set_"))
        {
          string propName = propMethName.Substring("?et_".Length);

          TypeDefinition calledTypeDef = null;
          if (calledMethodRef.DeclaringType is TypeDefinition)
            calledTypeDef = (TypeDefinition)calledMethodRef.DeclaringType;
          else
            calledTypeDef = calledMethodRef.DeclaringType.Resolve();
          ProcessDiscoveredProperty(propName, calledTypeDef, calledMethodRef, nomadMethodInfo);
        }
      }
      else
      {
        NomadMethodInfo calledNomadMethodInfo = new NomadMethodInfo(null, calledMethodRef);
        nomadMethodInfo.AddAccessedMethod(calledNomadMethodInfo);
        DiscoverNonLocalReferencesAndLinkedMethods(calledMethDef.DeclaringType, calledMethDef, calledNomadMethodInfo);
      }
    }

    private void ProcessDiscoveredProperty(string propertyName, TypeDefinition typeDef, MethodReference methodRef, NomadMethodInfo nomadMethodInfo)
    {
      if (!typeDef.HasProperties)
        throw new NomadException(string.Format("Method '{0}' references property '{1}' however type '{2}' has no declared properties", methodRef.FullName, propertyName, typeDef.FullName));
      PropertyReference typePropRef = typeDef.Properties.FirstOrDefault(p => p.Name == propertyName);
      if (typePropRef == default(PropertyReference))
        throw new NomadException(string.Format("Method '{0}' references property '{1}' however it is not declare on type '{2}'", methodRef.FullName, propertyName, typeDef.FullName));

      if (typePropRef is PropertyDefinition)
      {
        PropertyDefinition typePropDef = (PropertyDefinition)typePropRef;
        if (typePropDef.GetMethod != null)
        {
          NomadMethodInfo calledGetMethodInfo = new NomadMethodInfo(null, typePropDef.GetMethod);
          nomadMethodInfo.AddAccessedMethod(calledGetMethodInfo);
          DiscoverNonLocalReferencesAndLinkedMethods(typeDef, typePropDef.GetMethod, calledGetMethodInfo);
        }
        if (typePropDef.SetMethod != null)
        {
          NomadMethodInfo calledSetMethodInfo = new NomadMethodInfo(null, typePropDef.SetMethod);
          nomadMethodInfo.AddAccessedMethod(calledSetMethodInfo);
          DiscoverNonLocalReferencesAndLinkedMethods(typeDef, typePropDef.GetMethod, calledSetMethodInfo);
        }
      }
      nomadMethodInfo.AddAccessedProperty(typePropRef);
    }

    #endregion Methods
  }
}
