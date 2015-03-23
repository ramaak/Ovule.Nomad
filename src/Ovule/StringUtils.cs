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
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ovule
{
  public class StringUtils
  {
    public static bool IsValidEmailAddress(string potentialEmail)
    {
      if (string.IsNullOrEmpty(potentialEmail))
        return false;

      try
      {
        // Use IdnMapping class to convert Unicode domain names. 
        potentialEmail = Regex.Replace(potentialEmail, @"(@)(.+)$", DomainMapper, RegexOptions.None, TimeSpan.FromMilliseconds(200));
        return Regex.IsMatch(potentialEmail,
              @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
              @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
              RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
      }
      catch
      {
        return false;
      }
    }

    private static string DomainMapper(Match match)
    {
      IdnMapping idn = new IdnMapping();
      string domainName = match.Groups[2].Value;
      domainName = idn.GetAscii(domainName);
      return match.Groups[1].Value + domainName;
    }
  }
}
