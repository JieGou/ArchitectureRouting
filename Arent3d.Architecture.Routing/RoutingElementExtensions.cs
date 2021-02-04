using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;

namespace Arent3d.Architecture.Routing
{
  public static class RoutingElementExtensions
  {
    #region Initializations

    /// <summary>
    /// Confirms whether families and parameters used for routing application are loaded.
    /// </summary>
    /// <param name="document"></param>
    /// <returns>True if all families and parameters are loaded.</returns>
    public static bool RoutingSettingsAreInitialized( this Document document )
    {
      return document.AllRoutingFamiliesAreLoaded() && document.AllRoutingParametersAreRegistered() ;
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

    #region Connectors (General)

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
      return document.GetElement( new ElementId( elementId ) ).GetConnectorManager()?.Lookup( connectorId ) ;
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

    /// <summary>
    /// Directly connected connectors. For many cases, use <see cref="GetLogicallyConnectedConnectors"/>.
    /// </summary>
    /// <param name="connector"></param>
    /// <returns></returns>
    public static IEnumerable<Connector> GetConnectedConnectors( this Connector connector )
    {
      var id = connector.GetIndicator() ;
      return connector.AllRefs.OfType<Connector>().Where( c => c.GetIndicator() != id ) ;
    }

    /// <summary>
    /// Connectors beyond the fittings.
    /// </summary>
    /// <param name="connector"></param>
    /// <returns></returns>
    public static IEnumerable<Connector> GetLogicallyConnectedConnectors( this Connector connector )
    {
      foreach ( var conn in connector.GetConnectedConnectors() ) {
        if ( conn.Owner.IsFittingElement() ) {
          foreach ( var c in conn.GetOtherConnectorsInOwner().SelectMany( GetLogicallyConnectedConnectors ) ) yield return c ;
        }
        else {
          yield return conn ;
        }
      }
    }

    public static IEnumerable<Connector> OfEnd( this IEnumerable<Connector> connectors )
    {
      return connectors.Where( c => c.IsAnyEnd() ) ;
    }

    public static bool IsAnyEnd( this Connector conn )
    {
      return 0 != ( (int) conn.ConnectorType & (int) ConnectorType.AnyEnd ) ;
    }

    public static IEnumerable<Connector> GetOtherConnectorsInOwner( this Connector connector )
    {
      var id = connector.GetIndicator() ;
      var manager = connector.ConnectorManager ;
      if ( null == manager ) return Array.Empty<Connector>() ;

      return manager.Connectors.OfType<Connector>().Where( c => c.GetIndicator() != id ) ;
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

    #endregion

    #region Connectors (Routing)

    public static bool IsCompatibleTo( this Connector conn1, Connector conn2 )
    {
      return ( conn1.ConnectorType == conn2.ConnectorType ) && ( conn1.Domain == conn2.Domain ) && conn1.HasSameShape( conn2 ) ;
    }

    public static bool HasSameShape( this IConnector conn1, IConnector conn2 )
    {
      if ( conn1.Shape != conn2.Shape ) return false ;

      return true ;

      // // Concrete shape parameter can be different
      // return conn1.Shape switch
      // {
      //   ConnectorProfileType.Oval => HasSameOvalShape( conn1, conn2 ),
      //   ConnectorProfileType.Round => HasSameRoundShape( conn1, conn2 ),
      //   ConnectorProfileType.Rectangular => HasSameRectangularShape( conn1, conn2 ),
      //   _ => false,
      // } ;
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

    #region Pass Points

    public static FamilyInstance? FindPassPointElement( this Document document, int elementId )
    {
      var instance = document.GetElementById<FamilyInstance>( elementId ) ;
      if ( null == instance ) return null ;

      // TODO: check if family instance is a pass point family instance
      return instance ;
    }
    
    #endregion

    #region Routing (General)

    public static bool IsAutoRoutingGeneratedElement( this Element element )
    {
      return element.IsAutoRoutingGeneratedElementType() && element.HasParameter( RoutingParameter.RouteName ) ;
    }

    public static bool IsAutoRoutingGeneratedElementType( this Element element )
    {
      return element switch
      {
        Duct or Pipe or CableTray => true,
        _ => IsFittingElement( element ),
      } ;
    }

    public static bool IsFittingElement( this Element element )
    {
      var category = element.Category ;
      return ( category.CategoryType == CategoryType.Model && IsFittingCategory( category.GetBuiltInCategory() ) ) ;
    }

    private static bool IsFittingCategory( BuiltInCategory category )
    {
      return category switch
      {
        BuiltInCategory.OST_DuctFitting => true,
        BuiltInCategory.OST_PipeFitting => true,
        BuiltInCategory.OST_CableTrayFitting => true,
        BuiltInCategory.OST_ConduitFitting => true,
        _ => false,
      } ;
    }

    #endregion

    #region Routing (Route Names)

    private static readonly BuiltInCategory[] RoutingBuiltInCategories =
    {
      BuiltInCategory.OST_DuctFitting,
      BuiltInCategory.OST_DuctCurves,
      BuiltInCategory.OST_FlexDuctCurves,

      BuiltInCategory.OST_PipeFitting,
      BuiltInCategory.OST_PipeCurves,
      BuiltInCategory.OST_FlexPipeCurves,
    } ;

    public static IEnumerable<TElement> GetAllElementsOfRoute<TElement>( this Document document ) where TElement : Element
    {
      var parameterName = document.GetParameterName( RoutingParameter.RouteName ) ;
      if ( null == parameterName ) return Array.Empty<TElement>() ;

      var filter = new ElementParameterFilter( ParameterFilterRuleFactory.CreateSharedParameterApplicableRule( parameterName ) ) ;

      return RoutingBuiltInCategories.SelectMany( category => document.GetAllElementsOfRouteName<TElement>( category, filter ) ) ;
    }

    public static IEnumerable<TElement> GetAllElementsOfRouteName<TElement>( this Document document, string routeName ) where TElement : Element
    {
      var parameterName = document.GetParameterName( RoutingParameter.RouteName ) ;
      if ( null == parameterName ) return Array.Empty<TElement>() ;

      var filter = new ElementParameterFilter( ParameterFilterRuleFactory.CreateSharedParameterApplicableRule( parameterName ) ) ;

      return RoutingBuiltInCategories.SelectMany( category => document.GetAllElementsOfRouteName<TElement>( category, filter ).Where( e => e.GetRouteName() == routeName ) ) ;
    }

    private static IEnumerable<TElement> GetAllElementsOfRouteName<TElement>( this Document document, BuiltInCategory builtInCategory, ElementFilter filter ) where TElement : Element
    {
      if ( typeof( TElement ) == typeof( Element ) ) {
        return new FilteredElementCollector( document ).OfCategory( builtInCategory ).WhereElementIsNotElementType().WherePasses( filter ).OfType<TElement>() ;
      }
      else {
        return new FilteredElementCollector( document ).OfCategory( builtInCategory ).OfClass( typeof( TElement ) ).WhereElementIsNotElementType().WherePasses( filter ).OfType<TElement>() ;
      }
    }

    public static string? GetRouteName( this Element element )
    {
      if ( ! element.IsAutoRoutingGeneratedElement() ) return null ;
      if ( false == element.TryGetProperty( RoutingParameter.RouteName, out string? value ) ) return null ;
      return value ;
    }

    public static IEnumerable<Connector> CollectRoutingEndPointConnectors( this Document document, string routeName, bool fromConnector )
    {
      return document.GetAllElementsOfRouteName<MEPCurve>( routeName ).SelectMany( e => GetRoutingEndConnectors( e, fromConnector ) ) ;
    }
    public static (IReadOnlyCollection<Connector> From, IReadOnlyCollection<Connector>To) GetConnectors( this Document document, string routeName )
    {
      var fromList = document.CollectRoutingEndPointConnectors( routeName, true ).EnumerateAll() ;
      var toList = document.CollectRoutingEndPointConnectors( routeName, false ).EnumerateAll() ;
      return ( From: fromList, To: toList ) ;
    }

    public static IEnumerable<Connector> GetRoutingEndConnectors( this Element element, bool fromConnector )
    {
      return element.GetRoutingConnectors( fromConnector ).SelectMany( conn => conn.GetLogicallyConnectedConnectors().Where( IsRoutingEndConnector ) ) ;
    }
    private static bool IsRoutingEndConnector( Connector connector )
    {
      if ( ! connector.IsAnyEnd() ) return false ;
      if ( connector.Owner.IsAutoRoutingGeneratedElement() ) return false ;

      return true ;
    }

    #endregion

    #region Routing (From-To)

    public static void SetRoutingFromToConnectorIds( this Element element, IReadOnlyCollection<int> fromIds, IReadOnlyCollection<int> toIds )
    {
      element.SetProperty( RoutingParameter.FromSideConnectorIds, string.Join( "|", fromIds ) ) ;
      element.SetProperty( RoutingParameter.ToSideConnectorIds, string.Join( "|", toIds ) ) ;
    }

    public static IReadOnlyCollection<Connector> GetRoutingConnectors( this Element element, bool isFrom )
    {
      var manager = element.GetConnectorManager() ;
      if ( null == manager ) return Array.Empty<Connector>() ;

      var routingParam = ( isFrom ? RoutingParameter.FromSideConnectorIds : RoutingParameter.ToSideConnectorIds ) ;
      if ( false == element.TryGetProperty( routingParam, out string? value ) ) return Array.Empty<Connector>() ;
      if ( null == value ) return Array.Empty<Connector>() ;

      var list = new List<Connector>() ;
      foreach ( var s in value.Split( '|' ) ) {
        if ( false == int.TryParse( s, out var id ) ) continue ;

        var conn = manager.Lookup( id ) ;
        if ( null == conn ) continue ;

        list.Add( conn ) ;
      }

      return list ;
    }

    public static bool IsRoutingConnector( this Connector connector, bool isFrom )
    {
      var routingParam = ( isFrom ? RoutingParameter.FromSideConnectorIds : RoutingParameter.ToSideConnectorIds ) ;
      if ( false == connector.Owner.TryGetProperty( routingParam, out string? value ) ) return false ;
      if ( null == value ) return false ;

      var targetId = connector.Id ;
      return value.Split( '|' ).Any( s => int.TryParse( s, out var id ) && id == targetId ) ;
    }

    public static IEnumerable<Route> CollectRoutes( this Document document )
    {
      var dic = new Dictionary<string, (List<ConnectorIndicator>, List<ConnectorIndicator>)>() ;
      foreach ( var e in document.GetAllElementsOfRoute<Element>() ) {
        var routeName = e.GetRouteName() ;
        if ( null == routeName ) continue ;

        if ( false == dic.TryGetValue( routeName, out var list ) ) {
          list = ( new List<ConnectorIndicator>(), new List<ConnectorIndicator>() ) ;
          dic.Add( routeName, list ) ;
        }

        list.Item1.AddRange( e.GetRoutingEndConnectors( true ).Select( c => c.GetIndicator() ) ) ;
        list.Item2.AddRange( e.GetRoutingEndConnectors( false ).Select( c => c.GetIndicator() ) ) ;
      }

      foreach ( var (routeName, (fromList, toList)) in dic ) {
        if ( 0 == fromList.Count || 0 == toList.Count ) continue ;

        var route = new Route( document, routeName ) ;

        var from1 = fromList[ 0 ] ;
        var to1 = toList[ 0 ] ;
        foreach ( var to in toList ) {
          route.RegisterConnectors( from1, to ) ;  // TODO: Pass points
        }
        foreach ( var from in fromList.Skip( 1 ) ) {
          route.RegisterConnectors( from, to1 ) ;  // TODO: Pass points
        }

        yield return route ;
      }
    }

    #endregion
  }
}