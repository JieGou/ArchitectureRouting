using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public static class PickCommandUtil
  {
    public static IDisposable SetTempColor( this UIDocument uiDocument, ConnectorPicker.IPickResult pickResult )
    {
      var tempColor = new TempColor( uiDocument.ActiveView, new Color( 0, 0, 255 ) ) ;
      uiDocument.Document.Transaction( "TransactionName.Commands.Routing.Common.ChangeColor".GetAppStringByKeyOrDefault( null ), t =>
      {
        tempColor.AddRange( pickResult.GetAllRelatedElements() ) ;
        return Result.Succeeded ;
      } ) ;
      return tempColor ;
    }

    public static void DisposeTempColor( this Document document, IDisposable tempColor )
    {
      document.Transaction( "TransactionName.Commands.Routing.Common.RevertColor".GetAppStringByKeyOrDefault( null ), t =>
      {
        tempColor.Dispose() ;
        return Result.Succeeded ;
      } ) ;
    }

    public static IEndPoint GetEndPoint( ConnectorPicker.IPickResult pickResult, ConnectorPicker.IPickResult anotherResult )
    {
      if ( pickResult.PickedConnector is { } connector ) return new ConnectorEndPoint( connector ) ;

      var element = pickResult.PickedElement ;
      var pos = pickResult.GetOrigin() ;
      var anotherPos = anotherResult.GetOrigin() ;
      var dir = GetPreferredDirection( pos, anotherPos ) ;
      var preferredRadius = ( pickResult.PickedConnector ?? anotherResult.PickedConnector )?.Radius ;

      return new TerminatePointEndPoint( element.Document, ElementId.InvalidElementId, pos, dir, preferredRadius, element.Id ) ;
    }

    private static XYZ GetPreferredDirection( XYZ pos, XYZ anotherPos )
    {
      var dir = anotherPos - pos ;

      double x = Math.Abs( dir.X ), y = Math.Abs( dir.Y ) ;
      if ( x < y ) {
        return ( 0 <= dir.Y ) ? XYZ.BasisY : -XYZ.BasisY ;
      }
      else {
        return ( 0 <= dir.X ) ? XYZ.BasisX : -XYZ.BasisX ;
      }
    }

    public static ConnectorPicker.IPickResult PickResultFromAnother( Route route, IEndPoint endPoint )
    {
      var (subRoute, anotherEndPoint) = GetOtherEndPoint( route, endPoint ) ;
      return new PseudoPickResult( subRoute, anotherEndPoint) ;
    }

    private static (SubRoute, IEndPoint) GetOtherEndPoint( Route route, IEndPoint endPoint )
    {
      var endPointSubRouteMap = new Dictionary<IEndPoint, (SubRoute? OfFrom, SubRoute? OfTo)>() ;
      foreach ( var subRoute in route.SubRoutes ) {
        // from-side
        foreach ( var fromEndPoint in subRoute.FromEndPoints ) {
          if ( endPointSubRouteMap.TryGetValue( fromEndPoint, out var tuple ) ) {
            endPointSubRouteMap[ fromEndPoint ] = ( subRoute, tuple.OfTo ) ;
          }
          else {
            endPointSubRouteMap.Add( fromEndPoint, ( subRoute, null ) ) ;
          }
        }

        // to-side
        foreach ( var toEndPoint in subRoute.ToEndPoints ) {
          if ( endPointSubRouteMap.TryGetValue( toEndPoint, out var tuple ) ) {
            endPointSubRouteMap[ toEndPoint ] = ( tuple.OfFrom, subRoute ) ;
          }
          else {
            endPointSubRouteMap.Add( toEndPoint, ( null, subRoute ) ) ;
          }
        }
      }

      // seek other end point
      if ( false == endPointSubRouteMap.TryGetValue( endPoint, out var ofFromTo ) ) throw new InvalidOperationException() ;

      if ( null != ofFromTo.OfFrom ) {
        return TrailFrom( endPointSubRouteMap, ofFromTo.OfFrom ) ?? throw new InvalidOperationException() ;
      }
      else {
        return TrailTo( endPointSubRouteMap, ofFromTo.OfTo! ) ?? throw new InvalidOperationException() ;
      }
    }

    private static (SubRoute, IEndPoint)? TrailFrom( Dictionary<IEndPoint, (SubRoute? OfFrom, SubRoute? OfTo)> endPointSubRouteMap, SubRoute subRoute )
    {
      foreach ( var toEndPoint in subRoute.ToEndPoints ) {
        if ( false == endPointSubRouteMap.TryGetValue( toEndPoint, out var tuple ) ) continue ;

        if ( null == tuple.OfFrom ) return ( subRoute, toEndPoint ) ;
        if ( TrailFrom( endPointSubRouteMap, tuple.OfTo ! ) is { } result ) return result ;
      }

      return null ;
    }

    private static (SubRoute, IEndPoint)? TrailTo( Dictionary<IEndPoint, (SubRoute? OfFrom, SubRoute? OfTo)> endPointSubRouteMap, SubRoute subRoute )
    {
      foreach ( var fromEndPoint in subRoute.FromEndPoints ) {
        if ( false == endPointSubRouteMap.TryGetValue( fromEndPoint, out var tuple ) ) continue ;

        if ( null == tuple.OfTo ) return ( subRoute, fromEndPoint ) ;
        if ( TrailTo( endPointSubRouteMap, tuple.OfFrom ! ) is { } result ) return result ;
      }

      return null ;
    }

    private class PseudoPickResult : ConnectorPicker.IPickResult
    {
      private readonly SubRoute _subRoute ;
      private readonly IEndPoint _endPoint ;
      
      public PseudoPickResult( SubRoute subRoute, IEndPoint endPoint )
      {
        _subRoute = subRoute ;
        _endPoint = endPoint ;
      }

      public IEnumerable<ElementId> GetAllRelatedElements() => Enumerable.Empty<ElementId>() ;

      public SubRoute? SubRoute => ( _endPoint as RouteEndPoint )?.GetSubRoute() ?? _subRoute ;
      public Element PickedElement => throw new InvalidOperationException() ;
      public Connector? PickedConnector => ( _endPoint as ConnectorEndPoint )?.GetConnector() ?? _subRoute.GetReferenceConnector() ;
      public XYZ GetOrigin() => _endPoint.RoutingStartPosition ;

      public bool IsCompatibleTo( Connector connector )
      {
        return ( PickedConnector ?? SubRoute?.GetReferenceConnector() ?? _subRoute.GetReferenceConnector() ).IsCompatibleTo( connector ) ;
      }

      public bool IsCompatibleTo( Element element )
      {
        return ( _subRoute.Route.RouteName != element.GetRouteName() ) ;
      }
    }
  }
}