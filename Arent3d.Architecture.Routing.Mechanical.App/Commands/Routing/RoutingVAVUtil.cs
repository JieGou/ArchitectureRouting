using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  public class RoutingVAVUtil
  {
    public static int GetAHUNumberOfAHU( Connector rootConnector )
    {
      var ahuNumberOfAHU = (int) AHUNumberType.Invalid ;
      if ( rootConnector.Owner is FamilyInstance parentElement && parentElement.IsFamilyInstanceOf( RoutingFamilyType.AHU_2367 ) ) {
        parentElement.TryGetProperty( AHUNumberParameter.AHUNumber, out ahuNumberOfAHU ) ;
        return ahuNumberOfAHU ;
      }

      var connectors = rootConnector.GetConnectedConnectors() ;
      foreach ( var connector in connectors ) {
        // Get all the connected elements
        var connectedElements = connector.Owner.GetConnectors().SelectMany( s => s.GetConnectedConnectors() ).Where( s => s.IsConnected ).Select( s => s.Owner ) ;
        // Get the AHU element
        var ahuElement = connectedElements.OfType<FamilyInstance>().FirstOrDefault( f => f.IsFamilyInstanceOf( RoutingFamilyType.AHU_2367 ) ) ;
        if ( ahuElement == null ) continue ;
        ahuElement.TryGetProperty( AHUNumberParameter.AHUNumber, out ahuNumberOfAHU ) ;
        break ;
      }

      return ahuNumberOfAHU ;
    }
  }
}