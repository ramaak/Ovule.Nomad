﻿/*
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

namespace Ovule.Nomad.Client
{
  public class NomadClientInitialisationException: NomadClientException
  {
    public NomadClientInitialisationException(string message) : base(message) { }
    public NomadClientInitialisationException(string message, params object[] formatArgs) : base(string.Format(message, formatArgs)) { }
    
    public NomadClientInitialisationException(string message, Exception innerException) : base(message, innerException) { }
    public NomadClientInitialisationException(Exception innerException, string message, params object[] formatArgs) : base(string.Format(message, formatArgs), innerException) { }
  }
}
