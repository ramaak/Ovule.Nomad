/*
        The MIT License (MIT)
 
        Copyright (c) 2015 Tony Di Nucci (tonydinucci[at]gmail[dot]com)
 
        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:
        
        The above copyright notice and this permission notice shall be included in
        all copies or substantial portions of the Software.
        
        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        THE SOFTWARE.
        
        (http://opensource.org/licenses/mit-license.php)
    */
   
using Ovule.Configuration;
using System.Collections.Generic;

namespace Ovule.Nomad.Server.Stock
{
  public class InboundEmailConfigurationCollection : IConfigurationCollection
  {
    public bool AreAllSettingsRequired { get { return true; } }

    public string InboundEmailHost { get; set; }
    public int InboundEmailPort { get; set; }
    public bool InboundEmailUseSsl { get; set; }
    public string InboundEmailUsername { get; set; }
    public string InboundEmailPassword { get; set; }

    public IList<string> GetValidationErrors()
    {
      //AreAllSettingsRequired == true and don't need anything special
      return null;
    }
  }

  public class OutboundEmailConfigurationCollection : IConfigurationCollection
  {
    public bool AreAllSettingsRequired { get { return true; } }

    public string OutboundEmailHost { get; set; }
    public int OutboundEmailPort { get; set; }
    public bool OutboundEmailUseSsl { get; set; }
    public string OutboundEmailUsername { get; set; }
    public string OutboundEmailPassword { get; set; }
    public string OutboundEmailFromAddress { get; set; }

    public IList<string> GetValidationErrors()
    {
      //AreAllSettingsRequired == true and don't need anything special
      return null;
    }
  }
}
