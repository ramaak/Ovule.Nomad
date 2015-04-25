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

namespace Ovule.Nomad.Server
{
  /// <summary>
  /// The contract for a Nomad Server.  
  /// A Nomad Server executes methods on behalf of a Nomad Client.  The methods must be executed on the server within the same context that they 
  /// would have executed if left on the client.  That is, all fields and properties available on the client must be transmitted to the server where 
  /// they are loaded into memory before the method is executed.  When the method executes it may operate on these variables and modify 
  /// them.  Once the method has finished executing on the server the server side execution context must be captured and transferred back to the client.  
  /// The client must then orientated itself into the position it would have been had execution occurred within it's own process.
  /// </summary>
  public interface INomadServer
  {
    /// <summary>
    /// The implementation of this method must execute the method requested.  It will set up the execution context using 'nonLocalVariables', call the method 
    /// passing in 'parameters' and return to the caller with a NomadicMethodResult.  The NomadicMethodResult contains the methods return value plus 
    /// current values for all non-local fields/properties that are referenced by the method, the method it calls, the methods they call, etc.
    /// </summary>
    /// <param name="methodType">The form of execution</param>
    /// <param name="assemblyFileName">The filename of the assembly that contains the method to execute, e.g. "MyFancyAssembly.dll"</param>
    /// <param name="assemblyFileHash">Used as a checksum to ensure both client and server are talking about the same assembly</param>
    /// <param name="typeFullName">The full name of the type that contains the method to execute, e.g. "MyFancyApplication.MyFancyType"</param>
    /// <param name="methodName">The name of the method to execute within Type 'typeFullName', e.g. "MyFancyMethod"</param>
    /// <param name="parameters">The parameters accepted by method 'methodName'. If the method does not require parameters then set as 'null'</param>
    /// <param name="nonLocalVariables">The non-local fields/properties that method 'methodName' (or any other method which 'methodName' calls) accesses</param>
    /// <returns>The results of executing method 'methodName', i.e. the methods return value and details of all non-local fields/properties that have been referenced/changed</returns>
    NomadMethodResult ExecuteNomadMethod(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables);

    /// <summary>
    /// The implementation of this method must execute the method requested.  It will set up the execution context using 'nonLocalVariables', call the method 
    /// passing in 'parameters' and return to the caller with a NomadicMethodResult.  The NomadicMethodResult contains the methods return value plus 
    /// current values for all non-local fields/properties that are referenced by the method, the method it calls, the methods they call, etc.
    /// 
    /// If the server is not aware of an assembly by name 'assemblyFilename' and with checksum 'assemblyFileHash' then it will expect the client 
    /// to pass it a copy, using this method.
    /// </summary>
    /// <param name="methodType">The form of execution</param>
    /// <param name="assemblyFileName">The filename of the assembly that contains the method to execute, e.g. "MyFancyAssembly.dll"</param>
    /// <param name="assemblyFileHash">Used as a checksum to ensure both client and server are talking about the same assembly</param>
    /// <param name="rawAssembly">The assembly (in binary form) to execute the method on</param>
    /// <param name="typeFullName">The full name of the type that contains the method to execute, e.g. "MyFancyApplication.MyFancyType"</param>
    /// <param name="methodName">The name of the method to execute within Type 'typeFullName', e.g. "MyFancyMethod"</param>
    /// <param name="parameters">The parameters accepted by method 'methodName'. If the method does not require parameters then set as 'null'</param>
    /// <param name="nonLocalVariables">The non-local fields/properties that method 'methodName' (or any other method which 'methodName' calls) accesses</param>
    /// <returns>The results of executing method 'methodName', i.e. the methods return value and details of all non-local fields/properties that have been referenced/changed</returns>
    NomadMethodResult ExecuteNomadMethod(NomadMethodType methodType, string assemblyFileName, string assemblyFileHash, byte[] rawAssembly, string typeFullName, string methodName, IList<ParameterVariable> parameters, IList<IVariable> nonLocalVariables);
  }
}
