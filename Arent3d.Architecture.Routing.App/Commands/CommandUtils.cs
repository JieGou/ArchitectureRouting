using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  public static class CommandUtils
  {
    public static void AlertBadConnectors( IReadOnlyCollection<Connector> badConnectors )
    {
      TaskDialog.Show( "Connection error", "Some elbows, tees and/or connectors could not be inserted.\n\n・" + string.Join( "\n・", badConnectors.Select( GetConnectorInfo ) ) ) ;
    }

    private static string GetConnectorInfo( Connector connector )
    {
      var count = connector.ConnectorManager.Connectors.Size ;
      return $"[{count switch { 2 => "Elbow", 3 => "Tee", 4 => "Cross", _ => throw new ArgumentException() }}] {connector.Origin}" ;
    }
  }
}