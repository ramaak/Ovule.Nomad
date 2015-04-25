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

namespace Ovule.Nomad.Wcf
{
  /// <summary>
  /// If Nomad is to be used in WCF settings then implement this interface in some assembly that will ship with both 
  /// the client and server.  The GetKnownTypes() method must return all types that will be sent over the wire so that 
  /// the DataContractSerializer knows how to serialise/deserialise everything.
  /// 
  /// N.B. If you don't implement this interface then Nomad will use binary serialisation (with base 64 encoding) which 
  /// will allow for the system to function however performance may lowered.
  /// </summary>
  public interface IWcfKnownTypeProvider
  {
    /// <summary>
    /// Implementation must return a collection of all types that the application will send the WCF channel.  
    /// Without this information the DataContractSerializer will not know how to serialise/deserialise all objects.
    /// </summary>
    /// <returns>All types that are known to a DataContractSerializer</returns>
    IEnumerable<Type> GetKnownTypes();
  }
}
