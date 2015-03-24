using System;

namespace Ovule.Nomad
{
  /// <summary>
  /// Decorate an assembly with this attribute where you want instances of all types to execute on a Nomad service.
  /// 
  /// Using this attribute is equivalent to marking all typs in an assembly with [NomadType] 
  /// </summary>
  [AttributeUsage(AttributeTargets.Assembly)]
  public class NomadAssemblyAttribute: Attribute
  {
  }
}
