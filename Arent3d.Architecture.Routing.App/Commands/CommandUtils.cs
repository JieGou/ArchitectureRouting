using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  public static class CommandUtils
  {
    public static void AlertBadConnectors( IEnumerable<Connector[]> badConnectorSet )
    {
      TaskDialog.Show( "Connection error", "Some elbows, tees and/or connectors could not be inserted.\n\n・" + string.Join( "\n・", badConnectorSet.Select( GetConnectorInfo ) ) ) ;
    }

    private static string GetConnectorInfo( Connector[] connectorSet )
    {
      var connectionType = connectorSet.Length switch { 2 => "Elbow", 3 => "Tee", 4 => "Cross", _ => throw new ArgumentException() } ;
      var connector = connectorSet.FirstOrDefault( c => c.IsValidObject ) ;
      var coords = ( null != connector ) ? GetCoordValue( connector.Owner.Document, connector.Origin ) : "(Deleted connectors)" ;
      return $"[{connectionType}] {coords}" ;
    }

    private static string GetCoordValue( Document document, XYZ pos )
    {
      return document.DisplayUnitSystem switch
      {
        DisplayUnit.METRIC => $"({pos.X.RevitUnitsToMeters()}, {pos.Y.RevitUnitsToMeters()}, {pos.Z.RevitUnitsToMeters()})",
        _ => $"({pos.X.RevitUnitsToFeet()}, {pos.Y.RevitUnitsToFeet()}, {pos.Z.RevitUnitsToFeet()})",
      } ;
    }

    private static double RevitUnitsToFeet( this double units )
    {
      return UnitUtils.ConvertFromInternalUnits( units, UnitTypeId.Feet ) ;
    }
  }
}