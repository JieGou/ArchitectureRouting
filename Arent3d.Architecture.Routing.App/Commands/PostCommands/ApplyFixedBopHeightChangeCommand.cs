using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using Arent3d.Architecture.Routing.App.Commands.Routing ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [DisplayName( "Apply FixedBopHeight Change" )]
  [Transaction( TransactionMode.Manual )]
  public class ApplyFixedBopHeightChangeCommand : RoutingCommandBase
  {
    private const string Guid = "49634D4D-EA07-45D0-AA9E-B32F4CAEFDB8" ;
    protected override string GetTransactionNameKey() => "TransactionName.Commands.PostCommands.ApplyFixedBopHeightChangeCommand" ;
    
    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsParallelToTransaction( UIDocument uiDocument )
    {
      if ( FixedBopHeightViewModel.TargetRoute is { } targetRoute ) {
        var connectorHeight = targetRoute.FirstFromConnector()?.GetConnector()?.Origin.Z ;
        foreach ( var subRoute in targetRoute.SubRoutes ) {
          subRoute.ChangeFixedBopHeight( UnitUtils.ConvertToInternalUnits(FixedBopHeightViewModel.TargetHeight, UnitTypeId.Millimeters  ) + connectorHeight);
        }
        
        return targetRoute.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
      }

      return base.GetRouteSegmentsParallelToTransaction( uiDocument ) ;
    }
  }
}