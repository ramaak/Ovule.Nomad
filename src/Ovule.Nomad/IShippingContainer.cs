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

namespace Ovule.Nomad
{
  /// <summary>
  /// N.B. This type is likely to be made obsolete fairly quickly
  /// 
  /// By default developers do not make a choice of where individual nomadic methods execute (this is down to setup).
  /// 
  /// If you want a method to execute on a particular machine then wrap any single argument in an instance of IShippingContainer.
  /// The server will notice this and extract the Destination endpoint.  It will then execute that method on the machine 
  /// at that endpoint.
  /// </summary>
  public interface IShippingContainer
  {
    /// <summary>
    /// The URI of the machine you wish the method accepting parameters of type IShippingContainer to execute on
    /// </summary>
    Uri Destination { get; }

    /// <summary>
    /// The argument you are wrapping in the IShippingContainer
    /// </summary>
    object Cargo { get; }
  }

  public interface IShippingContainer<T>: IShippingContainer
  {
    new T Cargo { get; }
  }
}
