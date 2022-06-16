using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class DetailtLimitRackSelectionFilter : ISelectionFilter
  {
    private const string BoundaryCableTrayLineStyleName = "BoundaryCableTray" ;

    public static ISelectionFilter Instance => new DetailtLimitRackSelectionFilter() ;
    
    public bool AllowElement( Element element )
    {
      if ( element is not CurveElement { LineStyle: GraphicsStyle detailLimitLintStyle } ) return false ;
      return detailLimitLintStyle.GraphicsStyleCategory.Name == BoundaryCableTrayLineStyleName ;
    }

    public bool AllowReference( Reference reference, XYZ position ) => false ;

  }
}