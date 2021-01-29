using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
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
        var fromConnector = ConnectorPicker.GetConnector( uiDocument, "Select the first connector" ) ;
        var toConnector = ConnectorPicker.GetConnector( uiDocument, "Select the second connector", fromConnector ) ;
        routeRecords.Add( new RouteRecord( "Picked", fromConnector.GetIndicator(), toConnector.GetIndicator(), 17299574 ) ) ;
      } ) ;

      foreach ( var record in routeRecords ) {
        yield return record ;
      }
    }
  }
}