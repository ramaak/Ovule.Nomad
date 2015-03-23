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
namespace Ovule.Configuration
{
  /// <summary>
  /// Implementation must take an IConfigurationCollection and populate it somehow.
  /// </summary>
  public interface IConfigurationInitialiser<T> where T: IConfigurationCollection, new()
  {
    T Initialise(System.Configuration.Configuration config);
  }
}
