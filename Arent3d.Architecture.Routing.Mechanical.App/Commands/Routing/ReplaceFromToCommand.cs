using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.ReplaceFromToCommand", DefaultString = "Replace\nFrom-To" )]
  [Image( "resources/ReplaceFromTo.png" )]
  public class ReplaceFromToCommand : ReplaceFromToCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.ReplaceFromTo" ;

    protected override AddInType GetAddInType()
    {
      return AddInType.Mechanical ;
    }

    protected override (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, bool newPickIsFrom )
    {
      return ( PickCommandUtil.CreateRouteEndPoint( newPickResult ), null ) ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view )
    {
      return new MechanicalRoutingExecutor( document, view ) ;
    }
  }
}