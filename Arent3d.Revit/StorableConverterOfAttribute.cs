using System ;

namespace Arent3d.Revit
{
  [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
  public class StorableConverterOfAttribute : Attribute
  {
    public Type TargetType { get ; }

    public StorableConverterOfAttribute( Type targetType )
    {
      TargetType = targetType ;
    }
  }
}