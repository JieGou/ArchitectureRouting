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
      if ( ! ( document.GetElementById<FamilyInstance>( elementId )?.MEPModel?.ConnectorManager is { } connectorManager ) ) return null ;

      return connectorManager.Connectors.OfType<Connector>().FirstOrDefault( c => c.Id == connectorId ) ;
    }
  }
}