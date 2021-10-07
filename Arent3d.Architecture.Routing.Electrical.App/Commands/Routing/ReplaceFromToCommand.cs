using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.ReplaceFromToCommand", DefaultString = "Replace\nFrom-To" )]
  [Image( "resources/ReplaceFromTo.png" )]
  public class ReplaceFromToCommand : ReplaceFromToCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.ReplaceFromTo" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( Route route, ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, bool newPickIsFrom )
    {
      return PickCommandUtil.CreateBranchingRouteEndPoint( newPickResult, anotherPickResult, new RouteProperty( route ), route.GetSystemClassificationInfo(), AppCommandSettings.FittingSizeCalculator, newPickIsFrom ) ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    private class RouteProperty : IRouteProperty
    {
      private readonly Route _route ;

      public RouteProperty( Route route ) => _route = route ;

      public MEPSystemType? GetSystemType() => _route.GetMEPSystemType() ;

      public MEPCurveType GetCurveType() => _route.UniqueCurveType ?? throw new InvalidOperationException() ;

      public double GetDiameter() => _route.UniqueDiameter ?? throw new InvalidOperationException() ;

      public bool GetRouteOnPipeSpace() => _route.UniqueIsRoutingOnPipeSpace ?? throw new InvalidOperationException() ;

      public FixedHeight? GetFromFixedHeight() => _route.UniqueFromFixedHeight ;

      public FixedHeight? GetToFixedHeight() => _route.UniqueToFixedHeight ;

      public AvoidType GetAvoidType() => _route.UniqueAvoidType ?? throw new InvalidOperationException() ;

      public Opening? GetShaft()
      {
        if ( _route.UniqueShaftElementId is not { } shaftElementId || ElementId.InvalidElementId == shaftElementId ) return null ;
        return _route.Document.GetElementById<Opening>( shaftElementId ) ;
      }
    }
  }
}