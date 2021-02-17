using System ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public static class RevitDisplayElementExtensions
  {
    public static FillPatternElement? GetSolidFillPattern( this Document document )
    {
      return document.GetAllElements<FillPatternElement>().FirstOrDefault( pattern => pattern.GetFillPattern().IsSolidFill ) ;
    }

    public static void SetOverriddenColor( this Element element, Color? color )
    {
      element.Document.ActiveView.SetOverriddenColor( element.Id, color ) ;
    }

    internal static void SetOverriddenColor( this View view, ElementId elementId, Color? color )
    {
      if ( null == color ) {
        var ogs = new OverrideGraphicSettings() ;
        view.SetElementOverrides( elementId, ogs ) ;
      }
      else {
        var pattern = view.Document.GetSolidFillPattern() ;

        var ogs = new OverrideGraphicSettings() ;
        ogs.SetSurfaceForegroundPatternColor( color ) ;
        ogs.SetProjectionLineColor( color ) ;
        if ( null != pattern ) {
          ogs.SetSurfaceForegroundPatternId( pattern.Id ) ;
        }

        view.SetElementOverrides( elementId, ogs ) ;
      }
    }
  }
}