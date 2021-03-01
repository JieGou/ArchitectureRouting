using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.CommandTermCaches ;
using Arent3d.Architecture.Routing.EndPoint ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Pick From-To" )]
  [Image( "resources/MEP.ico" )]
  public class PickRoutingCommand : RoutingCommandBase
  {
    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected override IAsyncEnumerable<RouteRecord>? ReadRouteRecords( UIDocument uiDocument )
    {
      return ReadRouteRecordsByPick( uiDocument ).EnumerateAll().ToAsyncEnumerable() ;
    }

    /// <summary>
    /// Returns hard-coded sample from-to records.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    private static IEnumerable<RouteRecord> ReadRouteRecordsByPick( UIDocument uiDocument )
    {
      var routeRecords = new List<RouteRecord>() ;
      UiThread.RevitUiDispatcher.Invoke( () =>
      {
        var routes = RouteCache.Get( uiDocument.Document ) ;

        var fromPickResult = ConnectorPicker.GetConnector( uiDocument, "Select the first connector", null ) ;
        var tempColor = SetTempColor( uiDocument, fromPickResult ) ;
        try {
          var toPickResult = ConnectorPicker.GetConnector( uiDocument, "Select the second connector", fromPickResult ) ;
          var fromIndicator = GetEndPointIndicator( fromPickResult, toPickResult ) ;
          var toIndicator = GetEndPointIndicator( toPickResult, fromPickResult ) ;

          if ( fromPickResult.SubRoute is { } subRoute1 ) {
            var splitter = new RouteSplitter( subRoute1, fromPickResult.PickedElement!, toIndicator, false ) ;
            routeRecords.AddRange( RouteRecordUtils.ToRouteRecords( subRoute1.Route ) ) ;
            routeRecords.AddRange( splitter.CreateInsertedRouteRecords( subRoute1.Route ) ) ;
          }
          else if ( toPickResult.SubRoute is { } subRoute2 ) {
            var splitter = new RouteSplitter( subRoute2, fromPickResult.PickedElement!, fromIndicator, true ) ;
            routeRecords.AddRange( RouteRecordUtils.ToRouteRecords( subRoute2.Route ) ) ;
            routeRecords.AddRange( splitter.CreateInsertedRouteRecords( subRoute2.Route ) ) ;
          }
          else {
            for ( var i = routes.Count + 1 ; ; ++i ) {
              var name = "Picked_" + i ;
              if ( routes.ContainsKey( name ) ) continue ;

              routeRecords.Add( new RouteRecord( name, fromIndicator, toIndicator ) ) ;
              break ;
            }
          }
        }
        finally {
          DisposeTempColor( uiDocument.Document, tempColor ) ;
        }
      } ) ;

      foreach ( var record in routeRecords ) {
        yield return record ;
      }
    }

    private static IDisposable SetTempColor( UIDocument uiDocument, ConnectorPicker.IPickResult pickResult )
    {
      using var transaction = new Transaction( uiDocument.Document ) ;
      try {
        transaction.Start( "Change Picked Element Color" ) ;
        
        var tempColor = new TempColor( uiDocument.ActiveView, new Color( 0, 0, 255 ) ) ;
        tempColor.AddRange( pickResult.GetAllRelatedElements() ) ;

        transaction.Commit() ;
        return tempColor ;
      }
      catch {
        transaction.RollBack() ;
        throw ;
      }
    }

    private static void DisposeTempColor( Document document, IDisposable tempColor )
    {
      using var transaction = new Transaction( document ) ;
      try {
        transaction.Start( "Revert Picked Element Color" ) ;

        tempColor.Dispose() ;

        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
        throw ;
      }
    }

    private static IEndPointIndicator GetEndPointIndicator( ConnectorPicker.IPickResult pickResult, ConnectorPicker.IPickResult anotherResult )
    {
      if ( pickResult.PickedConnector is { } connector ) return connector.GetIndicator() ;

      var element = pickResult.PickedElement ;
      var anotherPos = anotherResult.GetOrigin() ;
      if ( element.IsPassPoint() && element is Instance instance ) {
        return GetPassPointBranchIndicator( instance, anotherPos ) ;
      }
      else {
        return GetCoordinateIndicator( pickResult.GetOrigin(), anotherPos ) ;
      }
    }

    private static IEndPointIndicator GetPassPointBranchIndicator( Instance passPointInstance, XYZ anotherPos )
    {
      var transform = passPointInstance.GetTotalTransform() ;
      var dir = anotherPos - transform.Origin ;
      double cos = transform.BasisY.DotProduct( dir ), sin = transform.BasisZ.DotProduct( dir ) ;
      var angleDegree = ToNormalizedDegree( Math.Atan2( sin, cos ) ) ;

      return new PassPointBranchEndIndicator( passPointInstance.Id.IntegerValue, angleDegree ) ;
    }

    private static double ToNormalizedDegree( double radian )
    {
      var cornerCount = Math.Round( radian / ( 0.5 * Math.PI ) ) ;
      cornerCount -= Math.Floor( cornerCount / 4 ) * 4 ; // [0, 1, 2, 3]
      return 90 * cornerCount ;// [0, 90, 180, 270]
    }

    private static IEndPointIndicator GetCoordinateIndicator( XYZ origin, XYZ anotherPos )
    {
      var dir = anotherPos - origin ;

      double x = Math.Abs( dir.X ), y = Math.Abs( dir.Y ) ;
      if ( x < y ) {
        dir = ( 0 <= dir.Y ) ? XYZ.BasisY : -XYZ.BasisY ;
      }
      else {
        dir = ( 0 <= dir.X ) ? XYZ.BasisX : -XYZ.BasisX ;
      }

      return new CoordinateIndicator( origin, dir ) ;
    }


    private class RouteSplitter
    {
      private readonly RouteInfoDetector _detector ;
      private readonly IEndPointIndicator _newIndicator ;
      private readonly bool _newConnectorIsFromConnector ;

      public RouteSplitter( SubRoute subRoute, Element splitElement, IEndPointIndicator endPointIndicator, bool newConnectorIsFromConnector )
      {
        _detector = new RouteInfoDetector( subRoute, splitElement ) ;
        _newIndicator = endPointIndicator ;
        _newConnectorIsFromConnector = newConnectorIsFromConnector ;
      }

      public IEnumerable<RouteRecord> CreateInsertedRouteRecords( Route route )
      {
        foreach ( var info in route.RouteInfos ) {
          var index = _detector.GetPassedThroughPassPointIndex( info ) ;
          if ( index < 0 ) continue ;

          if ( _newConnectorIsFromConnector ) {
            yield return CreateInsertedRouteRecordFrom( route.RouteName, info, index, _newIndicator ) ;
          }
          else {
            yield return CreateInsertedRouteRecordTo( route.RouteName, info, index, _newIndicator ) ;
          }
        }
      }

      private static RouteRecord CreateInsertedRouteRecordFrom( string routeName, RouteInfo info, int index, IEndPointIndicator newIndicator )
      {
        return new RouteRecord( routeName, newIndicator, info.ToId, info.PassPoints.SubArray( index ) ) ;
      }

      private static RouteRecord CreateInsertedRouteRecordTo( string routeName, RouteInfo info, int index, IEndPointIndicator newIndicator )
      {
        return new RouteRecord( routeName, info.FromId, newIndicator, info.PassPoints.SubArray( 0, index ) ) ;
      }
    }
  }
}