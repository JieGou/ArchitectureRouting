using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using MathLib ;

namespace Arent3d.Architecture.Routing.App
{
  public static class PointOnRoutePicker
  {
    public class PickInfo
    {
      public Element Element { get ; }
      public XYZ Position { get ; }
      public XYZ RouteDirection { get ; }

      public Route Route => SubRoute.Route ;
      public SubRoute SubRoute { get ; }
      public Connector ReferenceConnector { get ; }

      public double Radius => GetRadius( ReferenceConnector ) ;

      public PickInfo( SubRoute subRoute, Element element, XYZ pos, XYZ dir )
      {
        SubRoute = subRoute ;
        Element = element ;
        Position = pos ;
        RouteDirection = dir ;

        ReferenceConnector = Route.GetReferenceConnector() ;
      }

      private static double GetRadius( Connector connector )
      {
        return connector.Shape switch
        {
          ConnectorProfileType.Round => connector.Radius,
          ConnectorProfileType.Oval => connector.Radius,
          ConnectorProfileType.Rectangular => 0.5 * new Vector2d( connector.Width, connector.Height ).magnitude,
          _ => 0,
        } ;
      }
    }

    public static PickInfo PickRoute( UIDocument uiDocument, string message, string? firstRouteId = null )
    {
      var document = uiDocument.Document ;

      var dic = document.GetAllStorables<Route>().ToDictionary( route => route.RouteId ) ;
      var filter = new RouteFilter( dic, ( null == firstRouteId ) ? null : elm => ( firstRouteId == elm.GetRouteName() ) ) ;

      while ( true ) {
        var pickedObject = uiDocument.Selection.PickObject( ObjectType.PointOnElement, filter, message ) ;

        var elm = document.GetElement( pickedObject.ElementId ) ;
        if ( elm?.GetRouteName() is not {} routeName ) continue ;
        if ( false == dic.TryGetValue( routeName, out var route ) ) continue ;

        var subRoute = route.GetSubRoute( elm.GetSubRouteIndex() ?? -1 ) ;
        if ( null == subRoute ) continue ;

        var (pos, dir) = GetPositionAndDirection( elm, pickedObject.GlobalPoint ) ;
        if ( null == pos ) continue ;

        return new PickInfo( subRoute, elm, pos, dir! ) ;
      }
    }

    private static (XYZ? Position, XYZ? Direction) GetPositionAndDirection( Element elm, XYZ position )
    {
      return elm switch
      {
        MEPCurve curve => GetNearestPointAndDirection( curve, position ),
        FamilyInstance fi => ToPositionAndDirection( fi.GetTotalTransform() ),
        _ => ( null, null ),
      } ;
    }

    private static (XYZ? Position, XYZ? Direction) GetNearestPointAndDirection( MEPCurve curve, XYZ position )
    {
      var from = curve.GetRoutingConnectors( true ).FirstOrDefault() ;
      if ( null == from ) return ( null, null ) ;
      var to = curve.GetRoutingConnectors( false ).FirstOrDefault() ;
      if ( null == to ) return ( null, null ) ;

      var o = from.Origin.To3d() ;
      var dir = to.Origin.To3d() - o ;
      if ( dir.sqrMagnitude < 1e-12 ) return ( null, null ) ;

      var line = new MathLib.Line( o, dir ) ;
      var dist = line.DistanceTo( position.To3d(), 0 ) ;
      return ( Position: dist.PointOnSelf.ToXYZ(), Direction: dir.normalized.ToXYZ() ) ;
    }

    private static (XYZ Position, XYZ Direction) ToPositionAndDirection( Transform transform )
    {
      return ( transform.Origin, transform.BasisZ ) ;
    }


    private class RouteFilter : ISelectionFilter
    {
      private readonly IReadOnlyDictionary<string, Route> _allRoutes ;
      private readonly Predicate<Element>? _predicate ;

      public RouteFilter( IReadOnlyDictionary<string, Route> allRoutes, Predicate<Element>? predicate )
      {
        _allRoutes = allRoutes ;
        _predicate = predicate ;
      }

      public bool AllowElement( Element elem )
      {
        var routeName = elem.GetRouteName() ;
        if ( null == routeName || false == _allRoutes.ContainsKey( routeName ) ) return false ;

        return ( null == _predicate ) || _predicate( elem ) ;
      }

      public bool AllowReference( Reference reference, XYZ position )
      {
        return true ;
      }
    }
  }
}