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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ovule;

namespace Test.Ovule
{
  [TestClass]
  public class StringUtilsTest
  {
    [TestMethod]
    public void IsValidEmailAddress()
    {
      string[] validAddresses = new string[]
      {
        "email@example.com",
        "firstname.lastname@example.com",
        "email@subdomain.example.co.uk",
        "firstname+lastname@example.net",
        "email@123.123.123.123",
        "email@[123.123.123.123]",
        "1234567890@example.com",
        "email@example-one.org",
        "email@example.name",
        "email@example.museum",
        "email@example.co.jp",
        "firstname-lastname@example.biz",
      };

      foreach (string validAddress in validAddresses)
        Assert.IsTrue(StringUtils.IsValidEmailAddress(validAddress), validAddress);

      string[] invalidAddresses = new string[]
      {
        "#@%^%#$@#$@#.com",
        "@example.com",
        "Joe Smith <email@example.com>",
        "email.example.com",
        "email@example@example.co.uk",
        ".email@example.com",
        "email.@example.net",
        "email..email@example.com",
        "あいうえお@example.com",
        "email@example.com (Joe Smith)",
        "email@example",
        "_______@example.com",
        "email@-example.com",
        "email@example..com",
        "Abc..123@example.com",
      };

      foreach (string invalidAddress in invalidAddresses)
        Assert.IsFalse(StringUtils.IsValidEmailAddress(invalidAddress), invalidAddress);
    }
  }
}
