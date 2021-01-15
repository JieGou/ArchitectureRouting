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
    public static IEnumerable<TElement> GetAllElements<TElement>( this Document document ) where TElement : Element
    {
      var filter = new ElementClassFilter( typeof( TElement ) ) ;
      return new FilteredElementCollector( document ).WherePasses( filter ).OfType<TElement>() ;
    }
    
    public static TElement? GetElementById<TElement>( this Document document, int elementId ) where TElement : Element
    {
      return document.GetElement( new ElementId( elementId ) ) as TElement ;
    }

    public static Connector? FindConnector( this Document document, int elementId, int connectorId )
    {
      return document.GetElement( new ElementId( elementId ) ).GetConnectorManager()?.GetConnectorById( connectorId ) ;
    }

    public static Connector? GetConnectorById( this ConnectorManager connectorManager, int connectorId )
    {
      return connectorManager.Connectors.GetConnectorById( connectorId ) ;
    }
    public static Connector? GetConnectorById( this ConnectorSet connectorSet, int connectorId )
    {
      return connectorSet.OfType<Connector>().FirstOrDefault( c => c.Id == connectorId ) ;
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