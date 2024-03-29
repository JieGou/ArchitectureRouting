﻿using System ;
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

  public abstract class ApplySelectedFromToChangesCommandBase : RoutingCommandBaseWithParam<ApplySelectedFromToChangesCommandParameter>
  {
    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( ApplySelectedFromToChangesCommandParameter args, Document document )
    {
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
        if ( newProperties.UseFromFixedHeight is { } useFromFixedHeight ) {
          if ( useFromFixedHeight ) {
            subRoute.ChangeFromFixedHeight( newProperties.FromFixedHeight ) ;
          }
          else {
            subRoute.ChangeFromFixedHeight( null ) ;
          }
        }
        if ( newProperties.UseToFixedHeight is { } useToFixedHeight ) {
          if ( useToFixedHeight ) {
            subRoute.ChangeToFixedHeight( newProperties.ToFixedHeight ) ;
          }
          else {
            subRoute.ChangeToFixedHeight( null ) ;
          }
        }

        //Change AvoidType
        if ( newProperties.AvoidType is { } avoidType ) {
          subRoute.ChangeAvoidType( avoidType ) ;
        }

        subRoute.ChangeShaftElement( newProperties.Shaft ) ;
      }

      return route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll() ;
    }
  }
}