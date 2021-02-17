using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.CommandTermCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Pick Start-End" )]
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

        var (fromConnector, fromElement) = ConnectorPicker.GetConnector( uiDocument, "Select the first connector" ) ;
        var tempColor = SetTempColor( uiDocument, routes, fromElement ) ;
        try {
          var (toConnector, toElement) = ConnectorPicker.GetConnector( uiDocument, "Select the second connector", fromConnector, fromElement.GetRouteName() ) ;

          if ( GetSubRoute( routes, fromElement ) is { } subRoute1 ) {
            var splitter = new RouteSplitter( subRoute1, fromElement, toConnector, false ) ;
            routeRecords.AddRange( RouteRecordUtils.ToRouteRecords( subRoute1.Route ) ) ;
            routeRecords.AddRange( splitter.CreateInsertedRouteRecords( subRoute1.Route ) ) ;
          }
          else if ( GetSubRoute( routes, toElement ) is { } subRoute2 ) {
            var splitter = new RouteSplitter( subRoute2, toElement, fromConnector, true ) ;
            routeRecords.AddRange( RouteRecordUtils.ToRouteRecords( subRoute2.Route ) ) ;
            routeRecords.AddRange( splitter.CreateInsertedRouteRecords( subRoute2.Route ) ) ;
          }
          else {
            for ( var i = routes.Count + 1 ; ; ++i ) {
              var name = "Picked_" + i ;
              if ( routes.ContainsKey( name ) ) continue ;

              routeRecords.Add( new RouteRecord( name, fromConnector.GetIndicator(), toConnector.GetIndicator() ) ) ;
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
    private static IDisposable SetTempColor( UIDocument uiDocument, RouteCache routes, Element element )
    {
      using var transaction = new Transaction( uiDocument.Document ) ;
      try {
        transaction.Start( "Change Picked Element Color" ) ;
        
        var tempColor = new TempColor( uiDocument.ActiveView, new Color( 0, 0, 255 ) ) ;
        tempColor.AddRange( GetRelatedElements( routes, element ) ) ;

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

    private static IEnumerable<ElementId> GetRelatedElements( RouteCache routes, Element element )
    {
      if ( GetSubRoute( routes, element ) is { } subRoute ) {
        return element.Document.GetAllElementsOfSubRoute<Element>( subRoute.Route.RouteId, subRoute.SubRouteIndex ).Select( e => e.Id ) ;
      }
      else {
        return new[] { element.Id } ;
      }
    }

    private static SubRoute? GetSubRoute( IReadOnlyDictionary<string, Route> routes, Element fromElement )
    {
      var routeName = fromElement.GetRouteName() ;
      if ( null == routeName ) return null ;

      var subRouteIndex = fromElement.GetSubRouteIndex() ;
      if ( null == subRouteIndex ) return null ;

      if ( false == routes.TryGetValue( routeName, out var route ) ) return null ;
      return route.GetSubRoute( subRouteIndex.Value ) ;
    }

    private class RouteSplitter
    {
      private readonly RouteInfoDetector _detector ;
      private readonly ConnectorIndicator _newConnectorIndicator ;
      private readonly bool _newConnectorIsFromConnector ;

      public RouteSplitter( SubRoute subRoute, Element splitElement, Connector newConnector, bool newConnectorIsFromConnector )
      {
        _detector = new RouteInfoDetector( subRoute, splitElement ) ;
        _newConnectorIndicator = newConnector.GetIndicator() ;
        _newConnectorIsFromConnector = newConnectorIsFromConnector ;
      }

      public IEnumerable<RouteRecord> CreateInsertedRouteRecords( Route route )
      {
        foreach ( var info in route.RouteInfos ) {
          var index = _detector.GetPassedThroughPassPointIndex( info ) ;
          if ( index < 0 ) continue ;

          if ( _newConnectorIsFromConnector ) {
            yield return CreateInsertedRouteRecordFrom( route.RouteId, info, index, _newConnectorIndicator ) ;
          }
          else {
            yield return CreateInsertedRouteRecordTo( route.RouteId, info, index, _newConnectorIndicator ) ;
          }
        }
      }

      private static RouteRecord CreateInsertedRouteRecordFrom( string routeName, RouteInfo info, int index, ConnectorIndicator newConnectorIndicator )
      {
        return new RouteRecord( routeName, newConnectorIndicator, info.ToId, info.PassPoints.SubArray( index ) ) ;
      }

      private static RouteRecord CreateInsertedRouteRecordTo( string routeName, RouteInfo info, int index, ConnectorIndicator newConnectorIndicator )
      {
        return new RouteRecord( routeName, info.FromId, newConnectorIndicator, info.PassPoints.SubArray( 0, index ) ) ;
      }
    }
  }
}