using System ;

namespace Arent3d.Revit
{
  public enum ImageType
  {
    Normal,
    Large,
    Tooltip,
  }
  
  [AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
  public class ImageAttribute : Attribute
  {
    public ImageAttribute( string resourceName )
    {
      ResourceName = resourceName ;
    }

    public ImageType ImageType { get ; set ; } = ImageType.Large ;

    public string ResourceName { get ; }
  }
}