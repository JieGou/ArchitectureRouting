using System.Collections ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class ConduitAndConduitFittingSelectionFilter : ISelectionFilter
  {
    public static ISelectionFilter Instance => new ConduitAndConduitFittingSelectionFilter() ;

    public bool AllowElement( Element element ) => IsConduitOrConduitFitting( element ) ;

    public bool AllowReference( Reference reference, XYZ position ) => false ;

    public static bool IsConduitOrConduitFitting( Element element )
    {
      var elementBuiltInCategory = element.GetBuiltInCategory() ;
      if ( ( (IList)BuiltInCategorySets.ElectricalRoutingElements ).Contains( elementBuiltInCategory ) ) return true ;
      return elementBuiltInCategory == BuiltInCategory.OST_ConduitFitting ;
    }
  }
}