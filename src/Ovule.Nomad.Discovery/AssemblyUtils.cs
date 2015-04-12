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
using Mono.Cecil;
using System.IO;

namespace Ovule.Nomad.Discovery
{
  /// <summary>
  /// At the minute the methods here just look in the clients directory for assemblies.
  /// TODO: update so looking in the same locations as the .Net loader, i.e. GAC, etc.
  /// </summary>
  public static class AssemblyUtils
  {
    public static string GetAssemblyFilename(string directory, AssemblyNameReference asmRef, bool throwExceptionIfNotFound = true)
    {
      string asmFilePath = null;
      if (asmRef != null)
        asmFilePath = GetAssemblyFilename(directory, asmRef.Name, throwExceptionIfNotFound);
      return asmFilePath;
    }

    public static string GetAssemblyFilename(string directory, string asmPartName, bool throwExceptionIfNotFound = true)
    {
      string asmFilePath = null;
      if (!string.IsNullOrWhiteSpace(directory) && !string.IsNullOrWhiteSpace(asmPartName))
      {
        foreach (string file in Directory.EnumerateFiles(directory, string.Format("{0}.*", asmPartName)))
        {
          string filenameWithoutExt = file.ToLower();
          string ext = Path.GetExtension(filenameWithoutExt);
          filenameWithoutExt = Path.GetFileNameWithoutExtension(filenameWithoutExt);
          if (filenameWithoutExt == asmPartName.ToLower() && (ext == ".dll" || ext == ".exe"))
          {
            if (asmFilePath != null)
              throw new NomadException(string.Format("Cannot determine assembly file to pick for reference as more than one is possible.  Remove either {0}.exe or {0}.dll", asmPartName));
            asmFilePath = file;
          }
        }
        if (asmFilePath == null && throwExceptionIfNotFound)
          throw new FileNotFoundException(string.Format("Expected to find assembly with name like '{0}.*' in directory '{1}' but didn't", asmPartName, directory));
      }
      return asmFilePath;
    }
  }
}
