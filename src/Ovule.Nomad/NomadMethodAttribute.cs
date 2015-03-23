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
  /// The modes of operation for nomadic methods.
  /// 
  /// Normal = Method executes completely on server and after execution client is put into 
  /// position which it would have been had it executed the method itself.
  /// 
  /// Repeat = Method is executed on both the client and a server.
  /// 
  /// Relay = Method is executed on server completely and bypassed on the client. Similar 
  /// to 'Normal' in that the client resumes execution after the method however to the client
  /// it appears that the method was never called.
  /// </summary>
  public enum NomadMethodType { Normal, Repeat, Relay }

  /// <summary>
  /// Decorate a method with this attribute if you want it to execute on a Nomad Server.
  /// </summary>
  [AttributeUsage(AttributeTargets.Method)]
  public class NomadMethodAttribute : Attribute
  {
    public NomadMethodType MethodType { get; private set; }
    public bool RunInMainThread { get; private set; }
    /// <summary>
    /// For future use
    /// </summary>
    public bool IsAsync { get; private set; }

    public NomadMethodAttribute() : this(NomadMethodType.Normal, false) { }

    public NomadMethodAttribute(NomadMethodType methodType) : this(methodType, false) { }

    public NomadMethodAttribute(NomadMethodType methodType, bool runInMainThread)
      : base()
    {
      MethodType = methodType;
      RunInMainThread = runInMainThread;
    }
  }
}
