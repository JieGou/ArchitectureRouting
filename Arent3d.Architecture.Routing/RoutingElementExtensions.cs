using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoint ;
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

    public static IEnumerable<Connector> GetConnectors( this Element elm )
    {
      if ( ! ( elm.GetConnectorManager()?.Connectors is { } connectorSet ) ) return Array.Empty<Connector>() ;

      return connectorSet.OfType<Connector>() ;
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

      if ( instance.Symbol.Id != document.GetFamilySymbol( RoutingFamilyType.PassPoint )?.Id ) {
        // Family instance is not a pass point.
        return null ;
      }

      return instance ;
    }

    public static void SetPassPointConnectors( this Element element, IReadOnlyCollection<Connector> fromConnectors, IReadOnlyCollection<Connector> toConnectors )
    {
      element.SetProperty( RoutingParameter.PassPointNextToFromSideConnectorIds, string.Join( "|", fromConnectors.Select( c => c.GetIndicator().ToString() ) ) ) ;
      element.SetProperty( RoutingParameter.PassPointNextToToSideConnectorIds, string.Join( "|", toConnectors.Select( c => c.GetIndicator().ToString() ) ) ) ;
    }

    private static readonly char[] PassPointConnectorSeparator = { '|' } ;

    public static IEnumerable<ConnectorIndicator> GetPassPointConnectors( this Element element, bool isFrom )
    {
      var parameter = isFrom ? RoutingParameter.PassPointNextToFromSideConnectorIds : RoutingParameter.PassPointNextToToSideConnectorIds ;
      if ( false == element.TryGetProperty( parameter, out string? str ) ) return Array.Empty<ConnectorIndicator>() ;
      if ( string.IsNullOrEmpty( str ) ) return Array.Empty<ConnectorIndicator>() ;

      return str!.Split( PassPointConnectorSeparator, StringSplitOptions.RemoveEmptyEntries ).Select( ConnectorIndicator.Parse ).NonNull() ;
    }

    public static bool IsPassPoint( this Element element )
    {
      return element is FamilyInstance fi && fi.IsPassPoint() ;
    }

    public static bool IsPassPoint( this FamilyInstance element )
    {
      return element.IsRoutingFamilyInstanceOf( RoutingFamilyType.PassPoint ) ;
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
      if ( null == category ) return false ;
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

      BuiltInCategory.OST_MechanicalEquipment,  // pass point
    } ;

    public static IEnumerable<TElement> GetAllElementsOfRoute<TElement>( this Document document ) where TElement : Element
    {
      var parameterName = document.GetParameterName( RoutingParameter.RouteName ) ;
      if ( null == parameterName ) return Array.Empty<TElement>() ;

      var filter = new ElementParameterFilter( ParameterFilterRuleFactory.CreateSharedParameterApplicableRule( parameterName ) ) ;

      return document.GetAllElementsOfRouteName<TElement>( RoutingBuiltInCategories, filter ) ;
    }

    public static IEnumerable<TElement> GetAllElementsOfRouteName<TElement>( this Document document, string routeName ) where TElement : Element
    {
      var parameterName = document.GetParameterName( RoutingParameter.RouteName ) ;
      if ( null == parameterName ) return Array.Empty<TElement>() ;

      var filter = new ElementParameterFilter( ParameterFilterRuleFactory.CreateSharedParameterApplicableRule( parameterName ) ) ;

      return document.GetAllElementsOfRouteName<TElement>( RoutingBuiltInCategories, filter ).Where( e => e.GetRouteName() == routeName ) ;
    }

    public static IEnumerable<TElement> GetAllElementsOfSubRoute<TElement>( this Document document, string routeName, int subRouteIndex ) where TElement : Element
    {
      var routeNameParameterName = document.GetParameterName( RoutingParameter.RouteName ) ;
      if ( null == routeNameParameterName ) return Array.Empty<TElement>() ;

      var subRouteIndexParameterName = document.GetParameterName( RoutingParameter.SubRouteIndex ) ;
      if ( null == subRouteIndexParameterName ) return Array.Empty<TElement>() ;

      var filter = new ElementParameterFilter( new[]
      {
        ParameterFilterRuleFactory.CreateSharedParameterApplicableRule( routeNameParameterName ),
        ParameterFilterRuleFactory.CreateSharedParameterApplicableRule( subRouteIndexParameterName ),
      } ) ;

      return document.GetAllElementsOfRouteName<TElement>( RoutingBuiltInCategories, filter ).Where( e => e.GetRouteName() == routeName ).Where( e => e.GetSubRouteIndex() == subRouteIndex ) ;
    }

    private static IEnumerable<TElement> GetAllElementsOfRouteName<TElement>( this Document document, BuiltInCategory[] builtInCategories, ElementFilter filter ) where TElement : Element
    {
      return document.GetAllElements<Element>().OfCategory( builtInCategories ).OfNotElementType().Where( filter ).OfType<TElement>() ;
    }

    public static string? GetRouteName( this Element element )
    {
      if ( false == element.TryGetProperty( RoutingParameter.RouteName, out string? value ) ) return null ;
      return value ;
    }

    public static int? GetSubRouteIndex( this Element element )
    {
      if ( false == element.TryGetProperty( RoutingParameter.SubRouteIndex, out int value ) ) return null ;
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

    public static void SetRoutedElementFromToConnectorIds( this Element element, IReadOnlyCollection<int> fromIds, IReadOnlyCollection<int> toIds )
    {
      element.SetProperty( RoutingParameter.RoutedElementFromSideConnectorIds, string.Join( "|", fromIds ) ) ;
      element.SetProperty( RoutingParameter.RoutedElementToSideConnectorIds, string.Join( "|", toIds ) ) ;
    }

    public static IReadOnlyCollection<Connector> GetRoutingConnectors( this Element element, bool isFrom )
    {
      var manager = element.GetConnectorManager() ;
      if ( null == manager ) return Array.Empty<Connector>() ;

      var routingParam = ( isFrom ? RoutingParameter.RoutedElementFromSideConnectorIds : RoutingParameter.RoutedElementToSideConnectorIds ) ;
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
      var routingParam = ( isFrom ? RoutingParameter.RoutedElementFromSideConnectorIds : RoutingParameter.RoutedElementToSideConnectorIds ) ;
      if ( false == connector.Owner.TryGetProperty( routingParam, out string? value ) ) return false ;
      if ( null == value ) return false ;

      var targetId = connector.Id ;
      return value.Split( '|' ).Any( s => int.TryParse( s, out var id ) && id == targetId ) ;
    }

    public static IEnumerable<Route> CollectRoutes( this Document document )
    {
      return document.GetAllStorables<Route>() ;
    }

    public static IEnumerable<IEndPointIndicator> GetNearestEndPointIndicators( this Element element, bool isFrom )
    {
      if ( false == element.TryGetProperty( isFrom ? RoutingParameter.NearestFromSideEndPoints : RoutingParameter.NearestToSideEndPoints, out string? str ) ) {
        return Array.Empty<IEndPointIndicator>() ;
      }
      if ( null == str ) {
        return Array.Empty<IEndPointIndicator>() ;
      }

      return EndPointIndicator.ParseIndicatorList( str ) ;
    }

    #endregion

    #region Center Lines

    public static IEnumerable<Element> GetCenterLine( this Element element )
    {
      var document = element.Document ;
      return element.GetDependentElements( CenterLineFilter ).Select( document.GetElement ).Where( e => e.IsValidObject ) ;
    }

    private static readonly BuiltInCategory[] CenterLineCategories =
    {
      BuiltInCategory.OST_CenterLines,
      BuiltInCategory.OST_DuctCurvesCenterLine,
      BuiltInCategory.OST_DuctFittingCenterLine,
      BuiltInCategory.OST_FlexDuctCurvesCenterLine,
      BuiltInCategory.OST_PipeCurvesCenterLine,
      BuiltInCategory.OST_PipeFittingCenterLine,
      BuiltInCategory.OST_FlexPipeCurvesCenterLine,
    } ;
    private static readonly ElementFilter CenterLineFilter = new LogicalOrFilter( Array.ConvertAll( CenterLineCategories, CreateElementFilter ) ) ;

    private static ElementFilter CreateElementFilter( BuiltInCategory category ) => new ElementCategoryFilter( category ) ;

    #endregion
  }
}