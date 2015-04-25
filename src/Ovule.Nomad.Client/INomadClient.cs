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
using System;
using System.Collections.Generic;

namespace Ovule.Nomad.Client
{
  /// <summary>
  /// A contract for a Nomad Client.
  /// A Nomad Client delegates certain methods to a Nomad Server.  The methods must be executed on the server within the same context that they 
  /// would have executed if left on the client.  That is, all fields and properties available on the client must be transmitted to the server where 
  /// they are loaded into appropriate memory before the method is executed.  When the method executes it may operate on these variables and modify 
  /// them.  Once the method has finished executing on the server the server side execution context is captured and transferred back to the client.  
  /// The client must then orientated itself into the position it would have been had execution occurred soley on the client itself.
  /// </summary>
  public interface INomadClient
  {
    /// <summary>
    /// The implementation must make a request to a Nomad server to execute a particular method, within the context of a particular object.
    /// </summary>
    /// <param name="methodType">The form of method execution, i.e. Normal, Relay, Repeat, ...</param>
    /// <param name="actOn">The object from which the execution context for running method 'methodName' will come from</param>
    /// <param name="methodName">The method on 'actOn' that should be executed, but on the Nomad Server</param>
    /// <param name="parameters">The parameters to pass to method 'methodName' when it's executed</param>
    /// <returns></returns>
    ExecuteServiceCallResult ExecuteServiceCall(byte[] rawAssembly, Uri endpoint, NomadMethodType methodType, object actOn, string methodName, IList<ParameterVariable> parameters);
    ExecuteServiceCallResult ExecuteServiceCall(Uri endpoint, NomadMethodType methodType, object actOn, string methodName, IList<ParameterVariable> parameters);
    ExecuteServiceCallResult ExecuteServiceCall(NomadMethodType methodType, object actOn, string methodName, IList<ParameterVariable> parameters);

    /// <summary>
    /// The implementation must make a request to a Nomad server to execute a particular static method.
    /// </summary>
    /// <param name="methodType">The form of method execution, i.e. Normal, Relay, Repeat, ...</param>
    /// <param name="actOn">The object from which the execution context for running method 'methodName' will come from</param>
    /// <param name="methodName">The method on 'actOn' that should be executed, but on the Nomad Server</param>
    /// <param name="parameters">The parameters to pass to method 'methodName' when it's executed</param>
    /// <returns></returns>
    ExecuteServiceCallResult ExecuteStaticServiceCall(byte[] rawAssembly, Uri endpoint, NomadMethodType methodType, Type actOnType, string methodName, IList<ParameterVariable> parameters);
    ExecuteServiceCallResult ExecuteStaticServiceCall(Uri endpoint, NomadMethodType methodType, Type actOnType, string methodName, IList<ParameterVariable> parameters);
    ExecuteServiceCallResult ExecuteStaticServiceCall(NomadMethodType methodType, Type actOnType, string methodName, IList<ParameterVariable> parameters);
  }
}
