using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class LimitRackReferenceSelectionFilter : ISelectionFilter
  {
    public static ISelectionFilter Instance => new LimitRackReferenceSelectionFilter() ;

    public bool AllowElement( Element element ) => IsCableTrayOrCableTrayFitting( element ) || IsLimitRackBoundaryCurve(element);

    public bool AllowReference( Reference reference, XYZ position ) => false ;

    private static bool IsCableTrayOrCableTrayFitting( Element element )
    {
      var builtInCategory = element.GetBuiltInCategory() ;
      if ( builtInCategory != BuiltInCategory.OST_CableTrayFitting ) return false ;
      var paramName = "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( element.Document, "Rack Type" ) ;
      var comment = element.GetParameter( paramName )?.AsString() ;
      return !string.IsNullOrEmpty( comment ) &&  comment == NewRackCommandBase.RackTypes[ 1 ] ;
    }

    private static bool IsLimitRackBoundaryCurve(Element element)
    {
      if ( element is not CurveElement { LineStyle: GraphicsStyle detailLimitLintStyle } ) return false ;
      return detailLimitLintStyle.GraphicsStyleCategory.Name == EraseLimitRackCommandBase.BoundaryCableTrayLineStyleName ;
    }
  }
}