using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;

namespace Arent3d.Architecture.Routing
{
  public static class RoutingElementExtensions
  {
    #region Connectors and routings

    public static ConnectorIndicator GetIndicator( this Connector connector )
    {
      return new ConnectorIndicator( connector ) ;
    }

    public static Connector? FindConnector( this Document document, ConnectorIndicator ids )
    {
      return document.FindConnector( ids.ElementId, ids.ConnectorId ) ;
    }

    public static Connector? FindConnector( this Document document, int elementId, int connectorId )
    {
      return document.GetElement( new ElementId( elementId ) ).GetConnectorManager()?.GetConnectorById( connectorId ) ;
    }

    public static FamilyInstance? FindPassPointElement( this Document document, int elementId )
    {
      var instance = document.GetElementById<FamilyInstance>( elementId ) ;
      if ( null == instance ) return null ;

      // TODO: check if family instance is a pass point family instance
      return instance ;
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

    public static IEnumerable<Connector> GetConnectedConnectors( this Connector connector )
    {
      var id = connector.GetIndicator() ;
      return connector.AllRefs.OfType<Connector>().Where( c => c.GetIndicator() != id ) ;
    }

    public static IEnumerable<Connector> OfEnd( this IEnumerable<Connector> connectors )
    {
      return connectors.Where( c => c.ConnectorType == ConnectorType.End ) ;
    }

    public static IEnumerable<Connector> GetOtherConnectorsInOwner( this Connector connector )
    {
      var id = connector.GetIndicator() ;
      var manager = connector.ConnectorManager ;
      if ( null == manager ) return Array.Empty<Connector>() ;

      return manager.Connectors.OfType<Connector>().Where( c => c.GetIndicator() != id ) ;
    }

    public static bool IsAutoRoutingElement( this Element element )
    {
      return element.IsAutoRoutingElementType() && element.HasRoutingTarget() ;
    }

    public static bool IsAutoRoutingElementType( this Element element )
    {
      return element switch
      {
        Duct or Pipe or CableTray => true,
        _ => IsFittingElement( element ),
      } ;
    }

    public static bool HasRoutingTarget( this Element element )
    {
      return ( false == string.IsNullOrEmpty( element.GetPropertyString( RoutingParameter.RouteName ) ) ) ;
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

    public static string GetSystemTypeName( this Connector conn )
    {
      return conn.Domain switch
      {
        Domain.DomainPiping => conn.PipeSystemType.ToString(),
        Domain.DomainHvac => conn.DuctSystemType.ToString(),
        Domain.DomainElectrical => conn.ElectricalSystemType.ToString(),
        Domain.DomainCableTrayConduit => conn.ElectricalSystemType.ToString(),
        _ => string.Empty,
      } ;
    }

    public static bool IsCompatibleTo( this Connector conn1, Connector conn2 )
    {
      return ( conn1.ConnectorType == conn2.ConnectorType ) && ( conn1.Domain == conn2.Domain ) && conn1.HasSameShape( conn2 ) ;
    }

    public static bool HasSameShape( this IConnector conn1, IConnector conn2 )
    {
      if ( conn1.Shape != conn2.Shape ) return false ;

      return conn1.Shape switch
      {
        ConnectorProfileType.Oval => HasSameOvalShape( conn1, conn2 ),
        ConnectorProfileType.Round => HasSameRoundShape( conn1, conn2 ),
        ConnectorProfileType.Rectangular => HasSameRectangularShape( conn1, conn2 ),
        _ => false,
      } ;
    }

    private static bool HasSameOvalShape( IConnector conn1, IConnector conn2 )
    {
      // TODO
      return false ;
    }

    private static bool HasSameRoundShape( IConnector conn1, IConnector conn2 )
    {
      return MathComparisonUtils.IsAlmostEqual( conn1.Radius, conn2.Radius ) ;
    }
    private static bool HasSameRectangularShape( IConnector conn1, IConnector conn2 )
    {
      return MathComparisonUtils.IsAlmostEqual( conn1.Width, conn2.Width ) && MathComparisonUtils.IsAlmostEqual( conn1.Height, conn2.Height ) ;
    }
    
    #endregion

    #region Families and Properties

    /// <summary>
    /// Confirms whether families and parameters used for routing application are loaded.
    /// </summary>
    /// <param name="document"></param>
    /// <returns>True if all families and parameters are loaded.</returns>
    public static bool RoutingSettingsAreInitialized( this Document document )
    {
      return document.AllRoutingFamiliesAreLoaded() || document.AllParametersAreRegistered() ;
    }

    /// <summary>
    /// Setup all families and parameters used for routing application.
    /// </summary>
    /// <param name="document"></param>
    public static void SetupRoutingFamiliesAndParameters( this Document document )
    {
      if ( document.RoutingSettingsAreInitialized() ) return ;

      using var tx = new Transaction( document ) ;
      tx.Start( "Setup routing" ) ;
      try {
        document.MakeCertainAllRoutingFamilies() ;
        document.MakeCertainAllRoutingParameters() ;

        if ( false == document.RoutingSettingsAreInitialized() ) {
          throw new InvalidOperationException( "Failed to set up routing families and parameters." ) ;
        }

        tx.Commit() ;
      }
      catch ( Exception ) {
        tx.RollBack() ;
        throw ;
      }
    }

    #endregion
  }
}