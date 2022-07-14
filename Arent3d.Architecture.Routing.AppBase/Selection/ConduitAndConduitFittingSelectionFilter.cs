using System.Collections ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class ConduitAndConduitFittingSelectionFilter : ISelectionFilter
  {
    private const string BoundaryCableTrayLineStyleName = "BoundaryCableTray" ;

    public static ISelectionFilter Instance => new ConduitAndConduitFittingSelectionFilter() ;
    
    public bool AllowElement( Element element )
    {
      if ( IsConnector( element ) ) return true ;
      if ( IsConduitOrConduitFitting( element ) ) return true ;
      if ( element is not CurveElement { LineStyle: GraphicsStyle detailLimitLintStyle } ) return false ;
      return detailLimitLintStyle.GraphicsStyleCategory.Name == BoundaryCableTrayLineStyleName ;
    }

    public bool AllowReference( Reference reference, XYZ position ) => false ;

    public static bool IsConnector( Element element )
    {
      var elementBuiltInCategory = element.GetBuiltInCategory() ;
      return ( BuiltInCategory.OST_ElectricalFixtures == elementBuiltInCategory ||
               BuiltInCategory.OST_ElectricalEquipment == elementBuiltInCategory ) ;
    }

    public static bool IsConduitOrConduitFitting(Element element)
    {
      var elementBuiltInCategory = element.GetBuiltInCategory() ;
      if ( ( (IList)BuiltInCategorySets.ElectricalRoutingElements ).Contains( elementBuiltInCategory ) ) return true ;
      return elementBuiltInCategory == BuiltInCategory.OST_ConduitFitting ;
    }

  }
}