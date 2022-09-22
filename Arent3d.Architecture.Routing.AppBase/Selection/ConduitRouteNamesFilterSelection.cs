using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class ConduitRouteNamesSelectionFilter : ISelectionFilter
  {
    private Document Doc { get ;  }
    private IEnumerable<string>? RouteNames { get ; }

    public ConduitRouteNamesSelectionFilter( Document doc , IEnumerable<string>? routeNames = null )
    {
      Doc = doc ;
      RouteNames = routeNames ;
    }

    public bool AllowElement( Element element )
    {
      if ( BuiltInCategory.OST_Conduit != element.GetBuiltInCategory() )
        return false ;
      if ( RouteNames is null )
        return true ;
      return element.GetRouteName() is { } name && RouteNames.Contains( name ) ;
    }

    public bool AllowReference( Reference reference, XYZ position )
    {
      var element = Doc.GetElement( reference ) ;
      return AllowElement( element ) ;
    }
  }
}