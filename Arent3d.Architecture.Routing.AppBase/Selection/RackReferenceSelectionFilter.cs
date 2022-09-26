using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class RackReferenceSelectionFilter : ISelectionFilter
  {
    public static ISelectionFilter Instance => new RackReferenceSelectionFilter() ;

    public bool AllowElement( Element element ) => IsCableTrayOrCableTrayFitting( element ) ;

    public bool AllowReference( Reference reference, XYZ position ) => false ;

    private static bool IsCableTrayOrCableTrayFitting( Element element )
    {
      var builtInCategory = element.GetBuiltInCategory() ;
      if ( builtInCategory != BuiltInCategory.OST_CableTrayFitting ) 
        return false ;
      
      var paramName = "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( element.Document, "Rack Type" ) ;
      var comment = element.GetParameter( paramName )?.AsString() ;
      return !string.IsNullOrEmpty( comment ) &&  (comment == RackCommandBase.RackTypes[ 0 ] || comment == RackCommandBase.RackTypes[ 1 ] );
    }
  }
}