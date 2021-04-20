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
      var subRoute = SelectedFromToViewModel.TargetSubRoute ;
      var pickInfo = SelectedFromToViewModel.TargetPickInfo ;
      var diameters = SelectedFromToViewModel.Diameters ;
      var systemTypes = SelectedFromToViewModel.SystemTypes ;
      var curveTypes = SelectedFromToViewModel.CurveTypes ;

      if ( diameters != null && systemTypes != null && curveTypes != null ) {
        if ( route != null ) {
          //Change Diameter
          if (SelectedFromToViewModel.SelectedDiameterIndex != -1) {
            subRoute?.ChangePreferredNominalDiameter( diameters[ SelectedFromToViewModel.SelectedDiameterIndex ] ) ;
          }
          
          //Change SystemType
          subRoute?.ChangeSystemType( systemTypes[ SelectedFromToViewModel.SelectedSystemTypeIndex ] ) ;

          //Change CurveType
          if ( SelectedFromToViewModel.SelectedCurveTypeIndex != -1) {
            subRoute?.ChangeCurveType( curveTypes[ SelectedFromToViewModel.SelectedCurveTypeIndex ] ) ;
          }

          //ChangeDirect
          if ( SelectedFromToViewModel.IsDirect != null ) {
            subRoute?.ChangeIsRoutingOnPipeSpace( (bool) SelectedFromToViewModel.IsDirect ) ;
          }


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
          if ( SelectedFromToViewModel.IsDirect != null ) {
            pickInfo.SubRoute.ChangeIsRoutingOnPipeSpace( (bool) SelectedFromToViewModel.IsDirect ) ;
          }

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