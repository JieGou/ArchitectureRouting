using System ;

namespace Arent3d.Revit.UI.Attributes
{
  [AttributeUsage( AttributeTargets.Class )]
  public class PanelAttribute : Attribute
  {
    public string KeyString { get ; }

    public string TitleKey { get ; set ; } = string.Empty ;

    public PanelAttribute( string keyString )
    {
      KeyString = keyString ;
    }
  }
}