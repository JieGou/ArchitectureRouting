using System.Collections ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class ConduitAndConduitFittingSelectionFilter : ISelectionFilter
  {
    private const string BoundaryCableTrayLineStyleName = "BoundaryCableTray" ;

    public static ISelectionFilter Instance => new ConduitAndConduitFittingSelectionFilter() ;
    
    public bool AllowElement( Element element )
    {
      var elementBuitInCategory = element.GetBuiltInCategory() ;
      if ( IsConnector( element ) ) return true ;
      if ( ( (IList)BuiltInCategorySets.ElectricalRoutingElements ).Contains( elementBuitInCategory ) ) return true ;
      if ( elementBuitInCategory == BuiltInCategory.OST_ConduitFitting ) return true ;
      if ( element is not CurveElement { LineStyle: GraphicsStyle detailLimitLintStyle } ) return false ;
      return detailLimitLintStyle.GraphicsStyleCategory.Name == BoundaryCableTrayLineStyleName ;
    }

    public bool AllowReference( Reference reference, XYZ position ) => false ;

    public static bool IsConnector( Element element )
    {
      var elementBuitInCategory = element.GetBuiltInCategory() ;
      return ( BuiltInCategory.OST_ElectricalFixtures == elementBuitInCategory ||
               BuiltInCategory.OST_ElectricalEquipment == elementBuitInCategory ) ;
    }

  }
}