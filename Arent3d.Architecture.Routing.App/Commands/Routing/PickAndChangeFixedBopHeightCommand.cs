using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Routing.PickAndChangeFixedBopHeightCommand", DefaultString = "Change\nBopHeight" )]
  [Image( "resources/MEP.ico" )]
  public class PickAndChangeFixedBopHeightCommand : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.ApplyFixedBopHeightChangeCommand" ;

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsParallelToTransaction( UIDocument uiDocument )
    {
      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndChangeFixedBopHeight.Pick".GetAppStringByKeyOrDefault( null ) ) ;

      FixedBopHeightViewModel.ShowFixedBopHeightSettingDialog(uiDocument, pickInfo.Route);
      
      if ( FixedBopHeightViewModel.TargetRoute is { } targetRoute ) {
        ApplyNewFixedBopHeight(targetRoute);
        
        return targetRoute.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
      }
      return AsyncEnumerable.Empty<(string RouteName, RouteSegment Segment)>() ;
    }

    private void ApplyNewFixedBopHeight( Route route )
    {
      foreach ( var subRoute in route.SubRoutes ) {
        subRoute.ChangeFixedBopHeight(FixedBopHeightViewModel.TargetHeight);
      }
    }
  }
}
