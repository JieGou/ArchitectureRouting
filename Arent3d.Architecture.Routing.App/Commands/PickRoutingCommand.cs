using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
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
    private static int _index = 0 ;
    
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
        var (fromConnector, fromRouteName) = ConnectorPicker.GetConnector( uiDocument, "Select the first connector" ) ;
        var (toConnector, toRouteName) = ConnectorPicker.GetConnector( uiDocument, "Select the second connector", fromConnector, fromRouteName ) ;

        if ( null != fromRouteName ) {
          var (fromList, toList) = uiDocument.Document.GetConnectors( fromRouteName ) ;
          routeRecords.AddRange( RouteRecordUtils.ToRouteRecords( fromRouteName, ToIndicatorList( fromList ), ToIndicatorList( toList, fromConnector ) ) ) ;
        }
        else if ( null != toRouteName ) {
          var (fromList, toList) = uiDocument.Document.GetConnectors( toRouteName ) ;
          routeRecords.AddRange( RouteRecordUtils.ToRouteRecords( toRouteName, ToIndicatorList( fromList, fromConnector ), ToIndicatorList( toList ) ) ) ;
        }
        else {
          routeRecords.Add( new RouteRecord( $"Picked_{++_index}", fromConnector.GetIndicator(), toConnector.GetIndicator() ) ) ;
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