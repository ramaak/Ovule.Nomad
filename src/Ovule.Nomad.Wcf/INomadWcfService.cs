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
  /// The sole Nomad service contract.  The implementation should provide a route into an implementation of Ovule.Nomad.Server.INomadService
  /// </summary>
  [ServiceKnownType("GetKnownTypes", typeof(KnownTypeLocator))]
  [ServiceContract]
  public interface INomadWcfService
  {
    /// <summary>
    /// Refer to comments for Ovule.Nomad.Server.INomadServer.ExecuteNomadicMethod(...)
    /// </summary>
    /// <param name="assemblyFileName"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <param name="nonLocalVariables"></param>
    /// <returns></returns>
    [OperationContract]
    NomadMethodResult ExecuteNomadMethod(NomadMethodType methodType, bool runInMainThread, string assemblyFileName, string assemblyFileHash, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables);

    /// <summary>
    /// Same as ExecuteNomadMethod(...) however accepts a raw assembly
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="runInMainThread"></param>
    /// <param name="rawAssembly"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="parameters"></param>
    /// <param name="nonLocalVariables"></param>
    /// <returns></returns>
    [OperationContract]
    NomadMethodResult ExecuteNomadMethodRaw(NomadMethodType methodType, bool runInMainThread, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables);

    /// <summary>
    /// Here for the same purpose as ExecuteNomadMethod however offers the opportunity for alternative forms of serialisation.
    /// Currently, if there are no ServiceKnownTypes then the BinaryFormatter is used on objects that aren't fully known at compile time, i.e. NomadMethodResult and Variable 
    /// and this method is called instead of ExecuteNomadMethod
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="runInMainThread"></param>
    /// <param name="assemblyFileName"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="serialisedParameters"></param>
    /// <param name="serialisedNonLocalVariables"></param>
    /// <returns></returns>
    [OperationContract]
    string ExecuteNomadMethodUsingBinarySerialiser(NomadMethodType methodType, bool runInMainThread, string assemblyFileName, string assemblyFileHash, string typeFullName, string methodName, IList<string> serialisedParameters, IList<string> serialisedNonLocalVariables);

    /// <summary>
    /// Same as ExecuteNomadMethodUsingBinarySerialiser(...) however accepts a raw assembly
    /// </summary>
    /// <param name="methodType"></param>
    /// <param name="runInMainThread"></param>
    /// <param name="rawAssembly"></param>
    /// <param name="typeFullName"></param>
    /// <param name="methodName"></param>
    /// <param name="serialisedParameters"></param>
    /// <param name="serialisedNonLocalVariables"></param>
    /// <returns></returns>
    [OperationContract]
    string ExecuteNomadMethodUsingBinarySerialiserRaw(NomadMethodType methodType, bool runInMainThread, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<string> serialisedParameters, IList<string> serialisedNonLocalVariables);
  }
}
