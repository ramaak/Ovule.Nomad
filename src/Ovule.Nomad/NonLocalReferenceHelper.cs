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
using Ovule.Diagnostics;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ovule.Nomad
{
  /// <summary>
  /// General utility methods that help working with fields and properties on objects.
  /// </summary>
  public class NonLocalReferenceHelper
  {
    #region Properties/Fields

    private static ILogger _logger = LoggerFactory.Create(typeof(NonLocalReferenceHelper).FullName);

    #endregion Properties/Fields

    #region Util

    /// <summary>
    /// Updates an objects fields and properties so that they match what's in 'nonLocalVariables'
    /// </summary>
    /// <param name="actOn">The object to update fields/properties on.  Should be mull when working with static members</param>
    /// <param name="actOnType">The type to update fields/properties on</param>
    /// <param name="nonLocalVariables">The collection of fields/properties to update</param>
    public static void SetNonLocalVariables(object actOn, Type actOnType, IList<IVariable> nonLocalVariables)
    {
      if (actOnType == null)
        throw new NullReferenceException("'actOnType' is null");

      _logger.LogInfo("SetupNonLocalVariables: Type '{0}', non-local variable count '{1}' ", actOnType.FullName, nonLocalVariables == null ? "0" : nonLocalVariables.Count.ToString());

      if (nonLocalVariables != null && nonLocalVariables.Count > 0)
      {
        foreach (IVariable variable in nonLocalVariables)
        {
          try
          {
            _logger.LogInfo("SetupNonLocalVariables: Setting variable '{0}' on type '{1}' to '{2}", variable.Name, actOnType.FullName, variable.Value == null ? "null" : variable.Value);
            if (actOn != null)
              variable.CopyTo(actOn);
            else
              variable.CopyTo(actOnType);
          }
          catch (PropertySetterUnavailableException ex)
          {
            _logger.LogError("Continuing with execution however: {0}", ex.Message);
          }
        }
      }
    }

    /// <summary>
    /// Updates 'nonLocalVariables' (by ref) so that it contains up to date values for the 'actOn' object.
    /// </summary>
    /// <param name="actOn"></param>
    /// <param name="nonLocalVariables"></param>
    public static void RecoverNonLocalVariables(object actOn, IList<IVariable> nonLocalVariables)
    {
      if (actOn == null)
        throw new NullReferenceException("'actOn' is null");

      Type actOnType = actOn.GetType();

      _logger.LogInfo("RecoverNonLocalVariables: Type '{0}', non-local variable count '{1}' ", actOnType.FullName, nonLocalVariables == null ? "0" : nonLocalVariables.Count.ToString());

      if (nonLocalVariables != null && nonLocalVariables.Count > 0)
      {
        foreach (IVariable variable in nonLocalVariables)
        {
          if (variable == null)
            _logger.LogError("RecoverNonLocalVariables: A non-local Variable has not been initialised for type '{0}'", actOnType.FullName);

          _logger.LogInfo("RecoverNonLocalVariables: Recovering value of variable '{0}' on type '{1}'", variable.Name, actOnType.FullName);
          if (actOn != null)
            variable.CopyFrom(actOn);
          else
            variable.CopyFrom(actOnType);
        }
      }
    }

    #endregion Util
  }
}
