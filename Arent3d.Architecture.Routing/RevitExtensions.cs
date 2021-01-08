using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Defines extension methods for Revit data.
  /// </summary>
  public static class RevitExtensions
  {
    public static TElement? GetElementById<TElement>( this Document document, int elementId ) where TElement : class
    {
      return document.GetElement( new ElementId( elementId ) ) as TElement ;
    }

    public static Connector? FindConnector( this Document document, int elementId, int connectorId )
    {
      var connectorManager = document.GetElement( new ElementId( elementId ) ).GetConnectorManager() ;
      return connectorManager?.Connectors.OfType<Connector>().FirstOrDefault( c => c.Id == connectorId ) ;
    }

    public static ConnectorManager? GetConnectorManager( this Element elm )
    {
      return elm switch
      {
        FamilyInstance fi => fi.MEPModel?.ConnectorManager,
        MEPSystem sys => sys.ConnectorManager,
        MEPCurve crv => crv.ConnectorManager,
        _ => null,
      } ;
    }

    public static ConnectorSet ToConnectorSet( this IEnumerable<Connector> connectors )
    {
      var connectorSet = new ConnectorSet() ;

      foreach ( var connector in connectors ) {
        connectorSet.Insert( connector ) ;
      }

      return connectorSet ;
    }
  }
}