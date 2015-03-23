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

namespace Ovule.Configuration
{
  /// <summary>
  /// An implementing class should declare properties with public setters that have the same 
  /// name as settings in the source.
  /// </summary>
  public interface IConfigurationCollection
  {
    /// <summary>
    /// Implementation should return true if all defined settings are required.
    /// The ConfigurationInitialiser will throw an exception if this is "treu" and values aren't found 
    /// for one or more settings
    /// </summary>
    bool AreAllSettingsRequired { get; }

    /// <summary>
    /// Perform any specific validation on settings after they have been populated.
    /// </summary>
    IList<string> GetValidationErrors();
  }
}
