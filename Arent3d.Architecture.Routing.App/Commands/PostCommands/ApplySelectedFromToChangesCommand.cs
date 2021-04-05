using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [DisplayName( "Apply Selected From-To Changes" )]
  [Transaction( TransactionMode.Manual )]
  public class ApplySelectedFromToChangesCommand : Routing.RoutingCommandBase
  {
    private const string Guid = "D1464970-1251-442F-8754-E59E293FBC9D" ;
    protected override string GetTransactionNameKey() => "TransactionName.Commands.PostCommands.ApplySelectedFromToChangesCommand" ;

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsBeforeTransaction( UIDocument uiDocument )
    {
      var route = SelectedFromToViewModel.TargetRoute ;
      var pickInfo = SelectedFromToViewModel.TargetPickInfo ;
      var diameters = SelectedFromToViewModel.Diameters ;
      var systemTypes = SelectedFromToViewModel.SystemTypes ;
      var curveTypes = SelectedFromToViewModel.CurveTypes ;

      if ( diameters != null && systemTypes != null && curveTypes != null ) {
        if ( route != null ) {
          //Change Diameter
          route.GetSubRoute( 0 )?.ChangePreferredNominalDiameter( diameters[ SelectedFromToViewModel.SelectedDiameterIndex ] ) ;
          ;

          //Change SystemType
          route.GetSubRoute( 0 )?.ChangeCurveType( curveTypes[ SelectedFromToViewModel.SelectedCurveTypeIndex ] ) ;

          //Change CurveType
          route.GetSubRoute( 0 )?.ChangeCurveType( curveTypes[ SelectedFromToViewModel.SelectedCurveTypeIndex ] ) ;

          //ChangeDirect
          route.GetSubRoute( 0 )?.ChangeIsRoutingOnPipeSpace( SelectedFromToViewModel.IsDirect ) ;

          return route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
        }
        else if ( pickInfo != null ) {
          //Change Diameter
          pickInfo.SubRoute.ChangePreferredNominalDiameter( diameters[ SelectedFromToViewModel.SelectedDiameterIndex ] ) ;

          //Change SystemType
          pickInfo.SubRoute.ChangeSystemType( systemTypes[ SelectedFromToViewModel.SelectedSystemTypeIndex ] ) ;

          //Change CurveType
          pickInfo.SubRoute.ChangeCurveType( curveTypes[ SelectedFromToViewModel.SelectedCurveTypeIndex ] ) ;

          //Change Direct
          pickInfo.SubRoute.ChangeIsRoutingOnPipeSpace( SelectedFromToViewModel.IsDirect ) ;

          return pickInfo.Route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
        }
        else {
          return base.GetRouteSegmentsInTransaction( uiDocument ) ;
        }
      }
      else {
        return base.GetRouteSegmentsInTransaction( uiDocument ) ;
      }
    }
  }
}