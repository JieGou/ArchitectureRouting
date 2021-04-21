using System.Collections.Generic ;
using System.ComponentModel ;
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
      if ( SelectedFromToViewModel.PropertySourceType is { } propertySource ) {
        var route = propertySource.TargetRoute ;
        var subRoutes = propertySource.TargetSubRoutes ;
        var pickInfo = SelectedFromToViewModel.TargetPickInfo ;
        var diameters = propertySource.Diameters ;
        var systemTypes = propertySource.SystemTypes ;
        var curveTypes = propertySource.CurveTypes ;

        if ( diameters != null && systemTypes != null && curveTypes != null ) {
          if ( route != null && subRoutes != null ) {
            foreach ( var subRoute in subRoutes ) {
              //Change Diameter
              if ( SelectedFromToViewModel.SelectedDiameterIndex != -1 ) {
                subRoute.ChangePreferredNominalDiameter( diameters[ SelectedFromToViewModel.SelectedDiameterIndex ] ) ;
              }

              //Change SystemType
              subRoute.ChangeSystemType( systemTypes[ SelectedFromToViewModel.SelectedSystemTypeIndex ] ) ;

              //Change CurveType
              if ( SelectedFromToViewModel.SelectedCurveTypeIndex != -1 ) {
                subRoute.ChangeCurveType( curveTypes[ SelectedFromToViewModel.SelectedCurveTypeIndex ] ) ;
              }

              //ChangeDirect
              if ( SelectedFromToViewModel.IsDirect is { } isDirect ) {
                subRoute.ChangeIsRoutingOnPipeSpace( isDirect ) ;
              }
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
            if ( SelectedFromToViewModel.IsDirect is { } isDirect ) {
              pickInfo.SubRoute.ChangeIsRoutingOnPipeSpace( isDirect ) ;
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
      else {
        return base.GetRouteSegmentsInTransaction( uiDocument ) ;
      }
    }
  }
}