using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
  public abstract class ApplySelectedFromToChangesCommandBase : RoutingCommandBase
  {
    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      if ( SelectedFromToViewModel.PropertySourceType is not { } propertySource ) return base.GetRouteSegments( document, state ) ;

      var route = propertySource.TargetRoute ;
      var subRoutes = propertySource.TargetSubRoutes ;
      if ( route == null || subRoutes == null ) return base.GetRouteSegments( document, state ) ;

      //Change SystemType
      route.SetMEPSystemType( SelectedFromToViewModel.SelectedSystemType ) ;

      foreach ( var subRoute in subRoutes ) {
        //Change Diameter
        if ( SelectedFromToViewModel.SelectedDiameter is { } selectedDiameter ) {
          subRoute.ChangePreferredNominalDiameter( selectedDiameter ) ;
        }

        //Change CurveType
        if ( SelectedFromToViewModel.SelectedCurveType is { } selectedCurveType ) {
          subRoute.SetMEPCurveType( selectedCurveType ) ;
        }

        //ChangeDirect
        if ( SelectedFromToViewModel.IsDirect is { } isDirect ) {
          subRoute.ChangeIsRoutingOnPipeSpace( isDirect ) ;
        }

        //Change FixedHeight
        if ( SelectedFromToViewModel.OnHeightSetting is { } ) {
          subRoute.ChangeFixedBopHeight( SelectedFromToViewModel.FixedHeight ) ;
        }

        //Change AvoidType
        subRoute.ChangeAvoidType( SelectedFromToViewModel.AvoidType ) ;
      }

      return route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll() ;
    }
  }
}