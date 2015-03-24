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
using Ovule.Nomad.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ovule.Nomad.Processor
{
  public abstract class MethodProcessor: IMethodProcessor
  {
    #region Abstract

    public abstract NomadMethodType NomadMethodTypeProcessed { get; }
    protected abstract void ThrowIfMethodUnacceptable(NomadMethodInfo nomadMethodInfo);
    protected abstract void InjectNomadServiceCall(Type clientType, MethodDefinition methodDef);

    #endregion Abstract

    #region IMethodProcessor

    public NomadMethodInfo Process(MethodDefinition methodDef, Type nomadClientType)
    {
      this.ThrowIfArgumentIsNull(() => methodDef);
 
      try
      {
        NomadMethodInfo nomadMethInfo = new NomadMethodInfo(NomadMethodTypeProcessed, methodDef);

        DiscoverNonLocalReferencesAndLinkedMethods(methodDef.DeclaringType, methodDef, nomadMethInfo);

        ThrowIfMethodUnacceptable(nomadMethInfo);

        InjectNomadServiceCall(nomadClientType, methodDef);
        return nomadMethInfo;
      }
      catch (MethodAlreadyProcessedException)
      {
        //this exception may well have originated from a deep recursive call where method names would make no sense to the user.  Rethrow with a meaningfull message
        throw new MethodAlreadyProcessedException(string.Format("The method '{0}' in assembly '{1}' has already been processed. " +
          "Make sure to pass assemblies through the Nomad Processor only once.", methodDef.FullName, methodDef.Module.Assembly.FullName));
      }
    }

    #endregion IMethodProcessor

    #region Injection

    /// <summary>
    /// Inject MSIL into nomadic method which copies the method parameters into a list, in preperation for passing these to the server
    /// </summary>
    /// <param name="methodDef"></param>
    /// <param name="ilProcessor"></param>
    /// <param name="referenceInstruction"></param>
    /// <returns></returns>
    protected VariableDefinition InjectNomadServiceCallParameters(MethodDefinition methodDef, ILProcessor ilProcessor, Instruction referenceInstruction)
    {
      this.ThrowIfArgumentIsNull(() => methodDef);
      this.ThrowIfArgumentIsNull(() => ilProcessor);
      this.ThrowIfArgumentIsNull(() => referenceInstruction);

      if (methodDef.HasParameters)
      {
        VariableDefinition paramVarDef = new VariableDefinition(methodDef.Module.Import(typeof(ParameterVariable)));
        VariableDefinition paramsListVarDef = new VariableDefinition(methodDef.Module.Import(typeof(List<ParameterVariable>)));

        methodDef.Body.Variables.Add(paramVarDef);
        methodDef.Body.Variables.Add(paramsListVarDef);

        ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Newobj, methodDef.Module.Import(typeof(List<ParameterVariable>).GetConstructor(System.Type.EmptyTypes))));
        ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Stloc, paramsListVarDef));
        int paramId = 0;
        foreach (ParameterDefinition paramDef in methodDef.Parameters)
        {
          //set things up for the call "new ParameterVariable(string, string, object)"
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Ldstr, string.Format("param{0}", ++paramId)));
          //... load arg to get type
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Ldarg, paramDef));
          if (paramDef.ParameterType.IsValueType)
            ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Box, paramDef.ParameterType));
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Call, methodDef.Module.Import(typeof(object).GetMethod("GetType"))));
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Callvirt, methodDef.Module.Import(typeof(Type).GetProperty("FullName").GetGetMethod())));
          //... load arg to pass to "new ParameterVariable(...)"
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Ldarg, paramDef));
          if (paramDef.ParameterType.IsValueType)
            ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Box, paramDef.ParameterType));

          //call "new ParameterVariable(...)
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Newobj, methodDef.Module.Import(typeof(ParameterVariable).
            GetConstructor(new Type[] { typeof(string), typeof(string), typeof(object) }))));
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Stloc, paramVarDef));

          //add param ParameterVariable to list
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Ldloc, paramsListVarDef));
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Ldloc, paramVarDef));
          ilProcessor.InsertBefore(referenceInstruction, ilProcessor.Create(OpCodes.Callvirt, methodDef.Module.Import(typeof(List<ParameterVariable>).GetMethod("Add"))));
        }
        return paramsListVarDef;
      }
      return null;
    }

    #endregion Injection

    #region Discovery

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

          if (calledMethDef.DeclaringType != null && calledMethDef.DeclaringType.FullName == typeof(NomadClient).FullName)
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

    #endregion Discovery
  }
}

