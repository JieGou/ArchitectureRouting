using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Selection ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Selection
{
  public class ConduitSelectionFilter : ISelectionFilter
  {
    public static ISelectionFilter Instance { get ; } = new ConduitSelectionFilter() ;

    private ConduitSelectionFilter()
    {
    }

    public bool AllowElement( Element elem )
    {
      return ( BuiltInCategorySets.Conduits.Any( p => p == elem.GetBuiltInCategory() )
               && elem is FamilyInstance or Conduit ) ;
    }

    public bool AllowReference( Reference reference, XYZ position ) => false ;
  }
  
  public class ConduitRouteNamesFilter : ISelectionFilter
  {
    private Document Doc { get ;  }
    private IEnumerable<string>? RouteNames { get ; }

    public ConduitRouteNamesFilter( Document doc , IEnumerable<string>? routeNames = null )
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