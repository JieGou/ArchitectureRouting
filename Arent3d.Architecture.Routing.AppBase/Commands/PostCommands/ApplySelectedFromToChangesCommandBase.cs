using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
  public class ApplySelectedFromToChangesCommandParameter
  {
    public Route TargetRoute { get ; }
    public IReadOnlyCollection<SubRoute> TargetSubRoutes { get ; }

    public RouteProperties NewRouteProperties { get ; }

    public ApplySelectedFromToChangesCommandParameter( Route targetRoute, IReadOnlyCollection<SubRoute> targetSubRoutes, RouteProperties routeProperties )
    {
      TargetRoute = targetRoute ;
      TargetSubRoutes = targetSubRoutes ;
      NewRouteProperties = routeProperties ;
    }
  }

  public abstract class ApplySelectedFromToChangesCommandBase : RoutingCommandBase
  {
    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      if ( CommandParameterStorage.Pop<ApplySelectedFromToChangesCommandParameter>() is not { } arg ) return ( false, null ) ;

      return ( true, arg ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      var args = state as ApplySelectedFromToChangesCommandParameter ?? throw new InvalidOperationException() ;

      var route = args.TargetRoute ;
      var subRoutes = args.TargetSubRoutes ;
      var newProperties = args.NewRouteProperties ;

      //Change SystemType
      route.SetMEPSystemType( newProperties.SystemType ) ;

      foreach ( var subRoute in subRoutes ) {
        //Change Diameter
        if ( newProperties.Diameter is { } selectedDiameter ) {
          subRoute.ChangePreferredNominalDiameter( selectedDiameter ) ;
        }

        //Change CurveType
        if ( newProperties.CurveType is { } selectedCurveType ) {
          subRoute.SetMEPCurveType( selectedCurveType ) ;
        }

        //ChangeDirect
        if ( newProperties.IsRouteOnPipeSpace is { } isRoutingOnPipeSpace ) {
          subRoute.ChangeIsRoutingOnPipeSpace( isRoutingOnPipeSpace ) ;
        }

        //Change FixedHeight
        if ( newProperties.UseFixedHeight is { } useFixedHeight ) {
          if ( useFixedHeight ) {
            subRoute.ChangeFixedBopHeight( newProperties.FixedHeight ) ;
          }
          else {
            subRoute.ChangeFixedBopHeight( null ) ;
          }
        }

        //Change AvoidType
        if ( newProperties.AvoidType is { } avoidType ) {
          subRoute.ChangeAvoidType( avoidType ) ;
        }
      }

      return route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll() ;
    }
  }
}