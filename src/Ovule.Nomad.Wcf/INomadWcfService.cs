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
using System.Collections.Generic;
using System.ServiceModel;

namespace Ovule.Nomad.Wcf
{
  /// <summary>
  /// The sole Nomad WCF service contract.  The implementation should provide a route into an implementation of <see cref="Ovule.Nomad.Server.INomadServer"/>
  /// </summary>
  [ServiceKnownType("GetKnownTypes", typeof(KnownTypeLocator))]
  [ServiceContract]
  public interface INomadWcfService
  {
    /// <summary>
    /// The implementation must facilitate the execution of a method within an assembly and type specified by the calling process.
    /// <see cref="Ovule.Nomad.Server.INomadServer"/>
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="assemblyFileName"></param>
    /// <param name="assemblyFileHash"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <param name="nonLocalVariables"></param>
    /// <returns>The result of executing the method</returns>
    [OperationContract]
    NomadMethodResult ExecuteNomadMethod(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables);

    /// <summary>
    /// The implementation must facilitate the execution of a method within an assembly and type specified by the calling process.
    /// If the server does not know about the assembly the client wants to execute functionality on then this method must be used and 
    /// the assembly bytes provided within the 'rawAssembly' parameter.
    /// <see cref="Ovule.Nomad.Server.INomadServer"/>
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="assemblyFileName"></param>
    /// <param name="assemblyFileHash"></param>
    /// <param name="rawAssembly"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <param name="nonLocalVariables"></param>
    /// <returns>The result of executing the method</returns>
    [OperationContract]
    NomadMethodResult ExecuteNomadMethodRaw(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables);

    /// <summary>
    /// Here for the same purpose as ExecuteNomadMethod(...) however offers the opportunity for alternative forms of serialisation.
    /// Currently, if there are no ServiceKnownTypes then the BinaryFormatter is used on objects that aren't fully known at compile time, i.e. NomadMethodResult and Variable 
    /// and this method is called instead of ExecuteNomadMethod
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="assemblyFileName"></param>
    /// <param name="assemblyFileHash"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="serialisedParameters"></param>
    /// <param name="serialisedNonLocalVariables"></param>
    /// <returns>A base 64 encoded representation of a <see cref="Ovule.Nomad.NomadMethodResult"/></returns>
    [OperationContract]
    string ExecuteNomadMethodUsingBinarySerialiser(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, string typeFullName, string methodName, IList<string> serialisedParameters, IList<string> serialisedNonLocalVariables);

    /// <summary>
    /// Same as ExecuteNomadMethodUsingBinarySerialiser(...) however accepts a raw assembly
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="assemblyFileName"></param>
    /// <param name="assemblyFileHash"></param>
    /// <param name="rawAssembly"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="serialisedParameters"></param>
    /// <param name="serialisedNonLocalVariables"></param>
    /// <returns>A base 64 encoded representation of a <see cref="Ovule.Nomad.NomadMethodResult"/></returns>
    [OperationContract]
    string ExecuteNomadMethodUsingBinarySerialiserRaw(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<string> serialisedParameters, IList<string> serialisedNonLocalVariables);
  }
}
