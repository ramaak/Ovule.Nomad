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
  public static class ObjectExtensions
  {
    #region General Argument Exception

    /// <summary>
    /// Processes 'arg' using the 'isValidCheck' Func, throwing an ArgumentException is result of 'arg' is deemed invalid.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    /// <param name="isValidCheck"></param>
    /// <param name="invalidReason"></param>
    public static void ThrowArgumentException<T>(this object source, Expression<Func<T>> arg, Func<object, bool> isValidCheck, string invalidReason = null)
    {
      string argName = arg.GetPropertyName();
      object argValue = arg.GetPropertyValue();
      if (string.IsNullOrWhiteSpace(argName))
        argName = "UNKNOWN";
      if (!isValidCheck(argValue))
        throw new ArgumentException(string.Format("The '{0}' argument is invalid. {1}", argName, invalidReason));
    }

    /// <summary>
    /// Throws ArgumentException if result of 'arg' is null
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentIsNull<T>(this object source, Expression<Func<T>> arg)
    {
      ThrowArgumentException(source, arg, (a) => { return a != null; }, "It must have a value.");
    }

    #endregion General Argument Exception

    #region String Argument Exception

    /// <summary>
    /// Throws ArgumentException is result of 'arg' is null or whitespace
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentIsNoValueString(this object source, Expression<Func<string>> arg)
    {
      ThrowArgumentException(source, arg, (a) => { return !string.IsNullOrWhiteSpace((string)a); }, "It must have a value.");
    }

    #endregion String Argument Exception

    #region Int Argument Exception

    // Overloading these methods for each numeric type to have compile time checking so people can't accidentally call these methods with a string for example

    /// <summary>
    /// Throws ArgumentException if result of arg is less than 0
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotPositiveOrZero(this object source, Expression<Func<int>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (int)a >= 0; }, "It must have a value greater than or equal to 0.");
    }

    /// <summary>
    /// Throws ArgumentException if result of arg is less than 1
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotPositive(this object source, Expression<Func<int>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (int)a > 0; }, "It must have a value greater than 0.");
    }

    /// <summary>
    /// Throws ArgumentException if result of arg is greater than 0
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotNegativeOrZero(this object source, Expression<Func<int>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (int)a <= 0; }, "It must have a value less than or equal to 0.");
    }

    /// <summary>
    /// Throws ArgumentException if result of arg is greater than -1
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotNegative(this object source, Expression<Func<int>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (int)a < 0; }, "It must have a value less than 0.");
    }

    #endregion Int Argument Exception

    #region Decimal Argument Exception

    // Overloading these methods for each numeric type to have compile time checking so people can't accidentally call these methods with a string for example

    /// <summary>
    /// Throws ArgumentException if result of arg is less than 0
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotPositiveOrZero(this object source, Expression<Func<decimal>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (decimal)a >= 0; }, "It must have a value greater than or equal to 0.");
    }

    /// <summary>
    /// Throws ArgumentException if result of arg is less than 1
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotPositive(this object source, Expression<Func<decimal>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (decimal)a > 0; }, "It must have a value greater than 0.");
    }

    /// <summary>
    /// Throws ArgumentException if result of arg is greater than 0
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotNegativeOrZero(this object source, Expression<Func<decimal>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (decimal)a <= 0; }, "It must have a value less than or equal to 0.");
    }

    /// <summary>
    /// Throws ArgumentException if result of arg is greater than -1
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotNegative(this object source, Expression<Func<decimal>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (decimal)a < 0; }, "It must have a value less than 0.");
    }

    #endregion Decimal Argument Exception

    #region Double Argument Exception

    // Overloading these methods for each numeric type to have compile time checking so people can't accidentally call these methods with a string for example

    /// <summary>
    /// Throws ArgumentException if result of arg is less than 0
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotPositiveOrZero(this object source, Expression<Func<double>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (double)a >= 0; }, "It must have a value greater than or equal to 0.");
    }

    /// <summary>
    /// Throws ArgumentException if result of arg is less than 1
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotPositive(this object source, Expression<Func<double>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (double)a > 0; }, "It must have a value greater than 0.");
    }

    /// <summary>
    /// Throws ArgumentException if result of arg is greater than 0
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotNegativeOrZero(this object source, Expression<Func<double>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (double)a <= 0; }, "It must have a value less than or equal to 0.");
    }

    /// <summary>
    /// Throws ArgumentException if result of arg is greater than -1
    /// </summary>
    /// <param name="source"></param>
    /// <param name="arg"></param>
    public static void ThrowIfArgumentNotNegative(this object source, Expression<Func<double>> arg)
    {
      ThrowIfArgumentIsNull(source, arg);
      ThrowArgumentException(source, arg, (a) => { return (double)a < 0; }, "It must have a value less than 0.");
    }

    #endregion Double Argument Exception
  }
}
