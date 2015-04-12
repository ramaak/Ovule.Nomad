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
  public class NomadMethodProcessor : MethodProcessor
  {
    private bool _runInMainThread;

    public override NomadMethodType NomadMethodTypeProcessed { get { return NomadMethodType.Normal; } }

    public NomadMethodProcessor(bool runInMainThread)
      : base()
    {
      _runInMainThread = runInMainThread;
    }

    protected override void ThrowIfMethodUnacceptable(NomadMethodInfo methInfo)
    {
    }

    protected override void InjectNomadServiceCall(Type clientType, MethodDefinition methodDef)
    {
      this.ThrowIfArgumentIsNull(() => clientType);
      this.ThrowIfArgumentIsNull(() => methodDef);

      bool isCallInjected = false;

      //add local variable to hold "ExecuteServiceCall" result
      VariableDefinition nomadResultVarDef = new VariableDefinition(methodDef.Module.Import(typeof(ExecuteServiceCallResult)));
      methodDef.Body.Variables.Add(nomadResultVarDef);

      ILProcessor ilProcessor = methodDef.Body.GetILProcessor();
      Instruction firstInstruction = methodDef.Body.Instructions.First();
      if (firstInstruction != null)
      {
        VariableDefinition paramsVarDef = InjectNomadServiceCallParameters(methodDef, ilProcessor, firstInstruction);

        //call new NomadClient().ExecuteServiceCall(NomadMethodType, bool, object,string,IList<ParameterVariable>)
        //or new NomadClient().ExecuteServiceCall(NomadMethodType, bool, Type, string, IList<ParameterVariable>)
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, methodDef.Module.Import(clientType.GetConstructor(System.Type.EmptyTypes))));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_I4, (int)NomadMethodType.Normal));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_I4, Convert.ToInt32(_runInMainThread)));

        //if this is a static method then we call new NomadClient().ExecuteStaticServiceCall(...) - and pass in the type rather than "this"
        if (methodDef.IsStatic)
        {
          MethodInfo getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(System.RuntimeTypeHandle) });
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldtoken, methodDef.DeclaringType));
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, methodDef.Module.Import(getTypeFromHandle)));
        }
        else 
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg_0));

        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldstr, methodDef.Name));
        if (paramsVarDef != null)
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, paramsVarDef));
        else
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldnull));

        if (methodDef.IsStatic)
        {
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Callvirt, methodDef.Module.Import(clientType.GetMethod("ExecuteStaticServiceCall",
          new[] { typeof(NomadMethodType), typeof(bool), typeof(Type), typeof(string), typeof(IList<ParameterVariable>) }))));
        }
        else
        {
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, methodDef.Module.Import(clientType.GetMethod("ExecuteServiceCall",
            new[] { typeof(NomadMethodType), typeof(bool), typeof(object), typeof(string), typeof(IList<ParameterVariable>) }))));
        }

        //nomadResultVarDef is now at top of stack, save this so can refer back to it later easily
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Stloc, nomadResultVarDef));

        //if nomadResultVarDef is null jump into normal client side code
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, nomadResultVarDef));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Brfalse, firstInstruction));

        //check if the method was executed on server, i.e. [nomadResultVarDef].IsExecuted == true.  If not then continue normal client side execution
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, nomadResultVarDef));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Callvirt, methodDef.Module.Import(typeof(ExecuteServiceCallResult).
          GetMethod("get_IsExecuted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Brfalse, firstInstruction));

        //get the return value from the server side method if we need it
        if (methodDef.ReturnType != null && methodDef.ReturnType.FullName != typeof(void).FullName)
        {
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, nomadResultVarDef));
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Callvirt, methodDef.Module.Import(typeof(ExecuteServiceCallResult).
            GetMethod("get_Result", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))));
          //need to unbox value types and optionally cast reference types to the return type.  unbox.any does both these things
          ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Unbox_Any, methodDef.ReturnType));
        }
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ret));
        isCallInjected = true;
      }
      if (!isCallInjected)
        throw new NomadException(string.Format("Failed to insert nomad service call into client method called '{0}", methodDef.FullName));
    }
  }
}
