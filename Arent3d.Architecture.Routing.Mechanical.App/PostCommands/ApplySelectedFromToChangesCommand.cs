using System.Collections.Generic ;
using System.ComponentModel ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [DisplayName( "Apply Selected From-To Changes" )]
  [Transaction( TransactionMode.Manual )]
  public class ApplySelectedFromToChangesCommand : Routing.RoutingCommandBase
  {
    private const string Guid = "1ED7E7D1-57F0-45EB-BDB7-29762A3F0963" ;

    protected override string GetTransactionNameKey() => "TransactionName.Commands.PostCommands.ApplySelectedFromToChangesCommand" ;

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsParallelToTransaction( UIDocument uiDocument )
    {
      if ( SelectedFromToViewModel.PropertySourceType is { } propertySource ) {
        var route = propertySource.TargetRoute ;
        var subRoutes = propertySource.TargetSubRoutes ;

        if ( route == null || subRoutes == null ) return base.GetRouteSegmentsParallelToTransaction( uiDocument ) ;
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

        return route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
      }

      return base.GetRouteSegmentsParallelToTransaction( uiDocument ) ;
    }
  }
}