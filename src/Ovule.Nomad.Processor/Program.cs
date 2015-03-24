using Ovule.Nomad.Client;
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
using Ovule.Nomad.Client.Email;
using System;

namespace Ovule.Nomad.Processor
{
  public class Program
  {
    private const string ServerPath = @"C:\Users\adinucci\Documents\Visual Studio 2013\Projects\Ovule.Nomad\Ovule.Nomad.Server.Stock\bin\Debug";

    //private static Type NomadClientType = typeof(NomadWcfClient);
    ////private static Type NomadClientType = typeof(NomadEmailClient);
    //private const string ClientPath = @"C:\Users\adinucci\Documents\Visual Studio 2013\Projects\Ovule.Nomad\Ovule.Nomad.Sample\bin\Debug";

    private static Type NomadClientType = typeof(NomadWcfClient);
    private const string ClientPath = @"C:\Users\adinucci\Documents\Visual Studio 2013\Projects\Ovule.Nomad.Sample\Ovule.Nomad.Sample.SemiRealistic.Client\bin\Debug";
    

    public static void Main(string[] args)
    {
      Console.WriteLine("Processing path: {0}", ClientPath);
      new ApplicationProcessor().Process(NomadClientType, ClientPath, ServerPath);
      Console.WriteLine("Done");
      Console.ReadLine();
    }
  }
}
