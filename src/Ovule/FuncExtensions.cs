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
using System.Linq.Expressions;

namespace Ovule
{
  public static class FuncExtensions
  {
    #region Property

    /// <summary>
    /// Gets name from the property expression
    /// </summary>
    /// <typeparam name="T">Class type - no need to provide any more (see example)</typeparam>
    /// <param name="propertyPointer">Property expression, for example (() => CallTypeRestrictionId)</param>
    /// <returns></returns>
    public static string GetPropertyName<T>(this Expression<Func<T>> propertyPointer)
    {
      if (propertyPointer == null)
        throw new ArgumentNullException("propertyPointer");

      var expression = propertyPointer.Body as MemberExpression;
      if (expression == null)
      {
        var unaryExpression = propertyPointer.Body as UnaryExpression;
        if (unaryExpression != null)
          expression = unaryExpression.Operand as MemberExpression;
      }

      if (expression == null)
        throw new ArgumentException("This extension method cannot be used in this context");

      return expression.Member.Name;
    }

    /// <summary>
    /// Gets name from the property expression
    /// </summary>
    /// <typeparam name="T">Class type - no need to provide any more (see example)</typeparam>
    /// <param name="propertyPointer">Property expression, for example (() => CallTypeRestrictionId)</param>
    /// <returns></returns>
    public static T GetPropertyValue<T>(this Expression<Func<T>> propertyPointer)
    {
      if (propertyPointer == null)
        throw new ArgumentNullException("propertyPointer");

      T value = propertyPointer.Compile()();
      return value;
    }

    #endregion Property
  }
}
