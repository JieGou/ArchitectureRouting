using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;

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
    public static Connector? FindConnector( this Document document, ConnectorIds ids )
    {
      return document.FindConnector( ids.ElementId, ids.ConnectorId ) ;
    }

    public static Connector? GetConnectorById( this ConnectorManager connectorManager, int connectorId )
    {
      return connectorManager.Connectors.GetConnectorById( connectorId ) ;
    }
    public static Connector? GetConnectorById( this ConnectorSet connectorSet, int connectorId )
    {
      return connectorSet.OfType<Connector>().FirstOrDefault( c => c.Id == connectorId ) ;
    }

    public static ConnectorIds GetId( this Connector connector )
    {
      return new ConnectorIds( connector ) ;
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

    public static IEnumerable<Connector> GetConnectedConnectors( this Connector connector )
    {
      var id = connector.GetId() ;
      return connector.AllRefs.OfType<Connector>().Where( c => c.GetId() != id ) ;
    }

    public static IEnumerable<Connector> OfEnd( this IEnumerable<Connector> connectors )
    {
      return connectors.Where( c => c.ConnectorType == ConnectorType.End ) ;
    }

    public static IEnumerable<Connector> GetOtherConnectorsInOwner( this Connector connector )
    {
      var id = connector.GetId() ;
      var manager = connector.ConnectorManager ;
      if ( null == manager ) return Array.Empty<Connector>() ;

      return manager.Connectors.OfType<Connector>().Where( c => c.GetId() != id ) ;
    }

    public static bool IsAutoRoutingElement( this Element element )
    {
      return element switch
      {
        Duct or Pipe or CableTray => true,
        _ => IsFittingElement( element ),
      } ;
    }

    private static bool IsFittingElement( Element element )
    {
      var category = element.Category ;
      return ( category.CategoryType == CategoryType.Model && IsFittingCategory( (BuiltInCategory) category.Id.IntegerValue ) ) ;
    }

    private static bool IsFittingCategory( BuiltInCategory category )
    {
      return category switch
      {
        BuiltInCategory.OST_DuctFitting => true,
        BuiltInCategory.OST_PipeFitting => true,
        BuiltInCategory.OST_CableTrayFitting => true,
        _ => false,
      } ;
    }
  }
}