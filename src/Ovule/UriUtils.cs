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

namespace Ovule
{
  public enum UriType { Http, Tcp, NamedPipe, Email }

  public class UriUtils
  {
    public static UriType GetType(Uri uri)
    {
      if (uri == null)
        throw new NullReferenceException("'uri' is null so cannot determine type");
      switch (uri.Scheme)
      {
        case "http":
          return UriType.Http;
        case "net.tcp":
          return UriType.Tcp;
        case "net.pipe":
          return UriType.NamedPipe;
        case "mailto":
          return UriType.Email;
        default:
          throw new UriFormatException("Unexpected URI scheme. Expected schemes are; 'http', 'net.tcp', 'net.pipe' and 'mailto'");
      }
    }
  }
}
