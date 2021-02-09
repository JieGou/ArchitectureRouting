using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
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
        var routes = uiDocument.Document.GetAllStorables<Route>().ToDictionary( route => route.RouteId ) ;

        var (fromConnector, fromRouteName) = ConnectorPicker.GetConnector( uiDocument, "Select the first connector" ) ;
        var (toConnector, toRouteName) = ConnectorPicker.GetConnector( uiDocument, "Select the second connector", fromConnector, fromRouteName ) ;

        if ( null != fromRouteName && routes.TryGetValue( fromRouteName, out var fromRoute ) && fromRoute.FirstFromConnector() is { } fromIndicator ) {
          routeRecords.AddRange( RouteRecordUtils.ToRouteRecords( fromRoute ) ) ;
          routeRecords.Add( new RouteRecord( fromRoute.RouteId, fromIndicator, toConnector.GetIndicator() ) ) ;
        }
        else if ( null != toRouteName && routes.TryGetValue( toRouteName, out var toRoute ) && toRoute.FirstToConnector() is { } toIndicator ) {
          routeRecords.AddRange( RouteRecordUtils.ToRouteRecords( toRoute ) ) ;
          routeRecords.Add( new RouteRecord( toRoute.RouteId, fromConnector.GetIndicator(), toIndicator ) ) ;
        }
        else {
          for ( var i = routes.Count + 1 ; ; ++i ) {
            var name = "Picked_" + i ;
            if ( routes.ContainsKey( name ) ) continue ;

            routeRecords.Add( new RouteRecord( name, fromConnector.GetIndicator(), toConnector.GetIndicator() ) ) ;
            break ;
          }
        }
      } ) ;

      foreach ( var record in routeRecords ) {
        yield return record ;
      }
    }

    private static IList<ConnectorIndicator> ToIndicatorList( IReadOnlyCollection<Connector> collection )
    {
      var list = new List<ConnectorIndicator>( collection.Count + 1 ) ;
      list.AddRange( ( (IEnumerable<Connector>) collection ).Select( RoutingElementExtensions.GetIndicator ) ) ;
      return list ;
    }

    private static IList<ConnectorIndicator> ToIndicatorList( IReadOnlyCollection<Connector> collection, Connector itemToAppend )
    {
      var list = ToIndicatorList( collection ) ;
      list.Add( itemToAppend.GetIndicator() ) ;
      return list ;
    }
  }
}