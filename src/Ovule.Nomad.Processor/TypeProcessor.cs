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
using System.Linq;

namespace Ovule.Nomad.Processor
{
  public class TypeProcessor : ITypeProcessor
  {
    #region ITypeProcessor

    /// <summary>
    /// Processes all nomadic elements of a particular type
    /// </summary>
    /// <param name="nomadClientType"></param>
    /// <param name="typeDef"></param>
    /// <param name="assemblyDef"></param>
    /// <returns></returns>
    public NomadTypeInfo Process(TypeDefinition typeDef, Type nomadClientType)
    {
      this.ThrowIfArgumentIsNull(() => nomadClientType);
      this.ThrowIfArgumentIsNull(() => typeDef);

      NomadTypeInfo typeInfo = null;
      if (typeDef.HasMethods)
      {
        IEnumerable<MethodDefinition> methDefs = typeDef.Methods.Where(m => m.HasCustomAttributes &&
          m.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.FullName == typeof(NomadMethodAttribute).FullName) != null);

        if (methDefs != null && methDefs.Any())
        {
          int methDefCount = methDefs.Count();
          for (int i = 0; i < methDefCount; i++)
          {
            MethodDefinition methDef = methDefs.ElementAt(i);
            CustomAttribute nomMethattrib = methDef.CustomAttributes.FirstOrDefault(ca => ca.AttributeType.FullName == typeof(NomadMethodAttribute).FullName);
            IMethodProcessor methodProcessor = GetMethodProcessor(nomMethattrib);

            NomadMethodInfo nomadMethInfo = methodProcessor.Process(methDef, nomadClientType);
            if (nomadMethInfo == null)
              throw new NomadTypeProcessorException("Failed to process Nomadic method '{0}' from type '{1}'", methDef.FullName, typeDef.FullName);

            if (typeInfo == null)
              typeInfo = new NomadTypeInfo(typeDef);
            typeInfo.AddAccessedMethod(nomadMethInfo);
          }
        }
      }
      return typeInfo;
    }

    #endregion ITypeProcessor

    #region Methods

    /// <summary>
    /// Return the right type of IMethodProcessor for a method defined with attribute 'attribute'
    /// </summary>
    /// <param name="attribute"></param>
    /// <returns></returns>
    private IMethodProcessor GetMethodProcessor(CustomAttribute attribute)
    {
      this.ThrowIfArgumentIsNull(() => attribute);

      if (attribute.AttributeType.FullName == typeof(NomadMethodAttribute).FullName)
      {
        NomadMethodType methType = NomadMethodType.Normal;
        bool runInMainThread = false;
        if (attribute.HasConstructorArguments)
        {
          foreach (CustomAttributeArgument arg in attribute.ConstructorArguments)
          {
            if (arg.Type.FullName == typeof(NomadMethodType).FullName)
              methType = (NomadMethodType)arg.Value;
            else if (arg.Type.FullName == typeof(bool).FullName)
              runInMainThread = (bool)arg.Value;
          }
        }

        if (methType == NomadMethodType.Normal)
          return new NomadMethodProcessor(runInMainThread);
        if (methType == NomadMethodType.Repeat)
          return new RepeatMethodProcessor(runInMainThread);
        if (methType == NomadMethodType.Relay)
          return new RelayMethodProcessor(runInMainThread);
      }
      return null;
    }

    #endregion Methods
  }
}