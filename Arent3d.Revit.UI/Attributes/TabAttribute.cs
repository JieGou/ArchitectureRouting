using System ;

namespace Arent3d.Revit.UI.Attributes
{
  [Flags]
  public enum TabVisibilityMode
  {
    None = 0x0,
    NormalDocument = 0x1,
    FamilyDocument = 0x2,
    Always = NormalDocument | FamilyDocument,
  }

  [AttributeUsage( AttributeTargets.Class )]
  public class TabAttribute : Attribute
  {
    public string TabNameKey { get ; }

    public TabVisibilityMode VisibilityMode { get ; set ; } = TabVisibilityMode.Always ;

    public TabAttribute( string tabNameKey )
    {
      TabNameKey = tabNameKey ;
    }
  }
}