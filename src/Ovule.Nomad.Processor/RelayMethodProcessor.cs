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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ovule.Nomad.Processor
{
  public class RelayMethodProcessor : MethodProcessor
  {
    private bool _runInMainThread;

    public override NomadMethodType NomadMethodTypeProcessed { get { return NomadMethodType.Relay; } }

    public RelayMethodProcessor(bool runInMainThread)
      : base()
    {
      _runInMainThread = runInMainThread;
    }

    protected override void ThrowIfMethodUnacceptable(NomadMethodInfo methInfo)
    {
      this.ThrowIfArgumentIsNull(() => methInfo);

      MethodDefinition methDef = methInfo.Method.Resolve();
      if (methDef == null)
        throw new NullReferenceException(string.Format("Failed to resolve method '{0}' from method reference", methInfo.Method.FullName));
      if (!methDef.IsStatic)
        throw new InvalidOperationException(string.Format("Method '{0}' cannot be a relay method because it is not declared as static", methInfo.Method.FullName));
      if (methInfo.Method.ReturnType.FullName != typeof(void).FullName)
        throw new InvalidOperationException(string.Format("Method '{0}' cannot be a relay method because it has a non void return type", methInfo.Method.FullName));
    }

    public ParameterDefinition InjectIsRelayCallParameter(MethodDefinition methDef)
    {
      ParameterDefinition isRelayCallParamDef = new ParameterDefinition(methDef.Module.TypeSystem.String);
      isRelayCallParamDef.IsOptional = true;
      isRelayCallParamDef.HasDefault = true;
      //TODO: Change this so the parameter is of a particular type rather than looking at it's value
      isRelayCallParamDef.Constant = Constants.IsRepeatMethodCallFalseValue;
      methDef.Parameters.Add(isRelayCallParamDef);

      return isRelayCallParamDef;
    }

    protected override void InjectNomadServiceCall(Type clientType, MethodDefinition methodDef)
    {
      this.ThrowIfArgumentIsNull(() => clientType);
      this.ThrowIfArgumentIsNull(() => methodDef);

      bool isCallInjected = false;
      List<Instruction> callParamsInstructions = new List<Instruction>();
      MethodDefinition newClientMethDef = new MethodDefinition(methodDef.Name, methodDef.Attributes, methodDef.ReturnType);
      ILProcessor ilProcessor = newClientMethDef.Body.GetILProcessor();

      methodDef.DeclaringType.Methods.Add(newClientMethDef);
      if (methodDef.HasParameters)
      {
        foreach (ParameterDefinition clientMethParamDef in methodDef.Parameters)
        {
          ParameterDefinition newParamDef = new ParameterDefinition(clientMethParamDef.Name, clientMethParamDef.Attributes, clientMethParamDef.ParameterType);
          newClientMethDef.Parameters.Add(newParamDef);

          callParamsInstructions.Add(ilProcessor.Create(OpCodes.Ldarg, newParamDef));
        }
      }
      ParameterDefinition isRelayCallParamDef = InjectIsRelayCallParameter(newClientMethDef);
      foreach (Instruction instruction in methodDef.Body.Instructions)
        ilProcessor.Append(instruction);

      methodDef.Body.Instructions.Clear();
      ilProcessor = methodDef.Body.GetILProcessor();
      if (callParamsInstructions.Any())
      {
        foreach (Instruction callParamInstr in callParamsInstructions)
          ilProcessor.Append(callParamInstr);
      }
      //TODO: Change this so the parameter is of a particular type rather than looking at it's value
      ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, Constants.IsRepeatMethodCallFalseValue));
      ilProcessor.Append(ilProcessor.Create(OpCodes.Call, newClientMethDef));
      ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));

      ilProcessor = newClientMethDef.Body.GetILProcessor();
      Instruction firstInstruction = newClientMethDef.Body.Instructions.First();
      if (firstInstruction != null)
      {
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg, isRelayCallParamDef));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldstr, Constants.IsRepeatMethodCallTrueValue));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Callvirt,
          newClientMethDef.Module.Import(typeof(string).GetMethod("Equals", new Type[] { typeof(string) }))));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Brtrue, firstInstruction));

        VariableDefinition paramsVarDef = InjectNomadServiceCallParameters(newClientMethDef, ilProcessor, firstInstruction);

        MethodInfo getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) });
        //call new NomadClient().ExecuteRepeatCall(NomadMethodType, bool, Type,string,IList<ParameterVariable>)
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, newClientMethDef.Module.Import(clientType.GetConstructor(System.Type.EmptyTypes))));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_I4, (int)NomadMethodType.Normal));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_I4, Convert.ToInt32(_runInMainThread)));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldtoken, newClientMethDef.DeclaringType));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, newClientMethDef.Module.Import(getTypeFromHandle)));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldstr, newClientMethDef.Name));
        if (paramsVarDef != null)
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, paramsVarDef));
        else
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldnull));

        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Callvirt, newClientMethDef.Module.Import(clientType.GetMethod("ExecuteStaticServiceCall",
          new[] { typeof(NomadMethodType), typeof(bool), typeof(Type), typeof(string), typeof(IList<ParameterVariable>) }))));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ret));
        isCallInjected = true;
      }
      if (!isCallInjected)
        throw new NomadException(string.Format("Failed to insert nomad service call into client method called '{0}", methodDef.FullName));
    }
  }
}