using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;
using Autodesk.Revit.DB.Structure ;

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
    public static bool SetupRoutingFamiliesAndParameters( this Document document )
    {
      document.MakeCertainAllRoutingFamilies() ;
      document.MakeCertainAllRoutingParameters() ;

      return document.RoutingSettingsAreInitialized() ;
    }

    #endregion

    #region Connectors (General)

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
    /// Returns connected connectors.
    /// </summary>
    /// <param name="connector"></param>
    /// <returns></returns>
    public static IEnumerable<Connector> GetConnectedConnectors( this Connector connector )
    {
      var id = connector.Owner.Id ;
      return connector.AllRefs.OfType<Connector>().Where( c => c.Owner.Id != id ) ;
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
      var id = connector.Id ;
      var manager = connector.ConnectorManager ;
      if ( null == manager ) return Array.Empty<Connector>() ;

      return manager.Connectors.OfType<Connector>().Where( c => c.Id != id ) ;
    }

    public static int GetSystemType( this Connector conn )
    {
      return conn.Domain switch
      {
        Domain.DomainPiping => (int) conn.PipeSystemType,
        Domain.DomainHvac => (int) conn.DuctSystemType,
        Domain.DomainElectrical => (int) conn.ElectricalSystemType,
        Domain.DomainCableTrayConduit => (int) MEPSystemClassification.CableTrayConduit,
        _ => (int) MEPSystemClassification.UndefinedSystemClassification,
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
      var document = GetDocument( conn1 ) ?? GetDocument( conn2 ) ?? throw new InvalidOperationException() ;
      var tole = document.Application.VertexTolerance ;
      return Math.Abs( conn1.Radius - conn2.Radius ) < tole ;
    }

    private static bool HasSameRectangularShape( IConnector conn1, IConnector conn2 )
    {
      var document = GetDocument( conn1 ) ?? GetDocument( conn2 ) ?? throw new InvalidOperationException() ;
      var tole = document.Application.VertexTolerance ;
      return Math.Abs( conn1.Width - conn2.Width ) < tole && Math.Abs( conn1.Height - conn2.Height ) < tole ;
    }

    private static Document? GetDocument( IConnector conn )
    {
      return conn switch
      {
        Connector c => c.Owner?.Document,
        Element ce => ce.Document,
        _ => null,
      } ;
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
      element.SetProperty( RoutingParameter.PassPointNextToFromSideConnectorIds, string.Join( "|", fromConnectors.Select( ConnectorEndPoint.BuildParameterString ) ) ) ;
      element.SetProperty( RoutingParameter.PassPointNextToToSideConnectorIds, string.Join( "|", toConnectors.Select( ConnectorEndPoint.BuildParameterString ) ) ) ;
    }

    private static readonly char[] PassPointConnectorSeparator = { '|' } ;

    public static IEnumerable<IEndPoint> GetPassPointConnectors( this Element element, bool isFrom )
    {
      var parameter = isFrom ? RoutingParameter.PassPointNextToFromSideConnectorIds : RoutingParameter.PassPointNextToToSideConnectorIds ;
      if ( false == element.TryGetProperty( parameter, out string? str ) ) return Array.Empty<IEndPoint>() ;
      if ( string.IsNullOrEmpty( str ) ) return Array.Empty<IEndPoint>() ;

      var document = element.Document ;
      return str!.Split( PassPointConnectorSeparator, StringSplitOptions.RemoveEmptyEntries ).Select( str => ConnectorEndPoint.ParseParameterString( document, str ) ).NonNull() ;
    }

    public static bool IsPassPoint( this Element element )
    {
      return element is FamilyInstance fi && fi.IsPassPoint() ;
    }

    public static bool IsPassPoint( this FamilyInstance element )
    {
      return element.IsRoutingFamilyInstanceOf( RoutingFamilyType.PassPoint ) || element.HasParameter( RoutingParameter.RelatedPassPointId ) ;
    }

    public static bool IsConnectorPoint( this FamilyInstance element )
    {
        return element.IsRoutingFamilyInstanceOf( RoutingFamilyType.ConnectorInPoint ) || element.IsRoutingFamilyInstanceOf( RoutingFamilyType.ConnectorOutPoint ) ||
                element.IsRoutingFamilyInstanceOf( RoutingFamilyType.ConnectorPoint ) || element.IsRoutingFamilyInstanceOf( RoutingFamilyType.TerminatePoint );
    }
    public static int? GetPassPointId( this Element element )
    {
      if ( element is not FamilyInstance fi ) return null ;

      if ( fi.IsRoutingFamilyInstanceOf( RoutingFamilyType.PassPoint ) ) return fi.Id.IntegerValue ;
      if ( element.TryGetProperty( RoutingParameter.RelatedPassPointId, out int id ) ) return id ;
      return null ;
    }

    public static FamilyInstance AddPassPoint( this Document document, string routeName, XYZ position, XYZ direction, double? radius )
    {
      var symbol = document.GetFamilySymbol( RoutingFamilyType.PassPoint )! ;
      if ( false == symbol.IsActive ) symbol.Activate() ;

      var instance = document.Create.NewFamilyInstance( position, symbol, StructuralType.NonStructural ) ;
      instance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( 0.0 ) ;
      if ( radius.HasValue ) {
        instance.LookupParameter( "Arent-RoundDuct-Diameter" ).Set( radius.Value * 2.0 ) ;
      }

      var elevationAngle = Math.Atan2( direction.Z, Math.Sqrt( direction.X * direction.X + direction.Y * direction.Y ) ) ;
      var rotationAngle = Math.Atan2( direction.Y, direction.X ) ;

      ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisY ), -elevationAngle ) ;
      ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisZ ), rotationAngle ) ;

      instance.SetProperty( RoutingParameter.RouteName, routeName ) ;
      
      return instance ;
    }

    public static FamilyInstance AddConnectorFamily( this Document document, Connector conn, string routeName, string? typeName, XYZ position, XYZ direction, double? radius )
    {
        var symbol = document.GetFamilySymbol( RoutingFamilyType.ConnectorPoint )!;
        if ( typeName == "イン" ) {
            symbol = document.GetFamilySymbol( RoutingFamilyType.ConnectorInPoint )!;
        }
        else if( typeName == "アウト"  ) {
            symbol = document.GetFamilySymbol( RoutingFamilyType.ConnectorOutPoint )!;
        }
        if ( false == symbol.IsActive )
            symbol.Activate();

        var instance = document.Create.NewFamilyInstance( position, symbol, StructuralType.NonStructural );
        instance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( 0.0 );
        int id = conn.Id;

        instance.SetProperty( RoutingParameter.RelatedTerminatePointId, id );
        //instance.LookupParameter( "Route Connector Relation Ids" ).Set( id );


        var elevationAngle = Math.Atan2( direction.Z, Math.Sqrt( direction.X * direction.X + direction.Y * direction.Y ) );
            Color colorIn = new Autodesk.Revit.DB.Color( (byte) 255, (byte) 0, (byte) 0 );
            Color colorOut = new Autodesk.Revit.DB.Color( (byte) 0, (byte) 0, (byte) 255 );
            OverrideGraphicSettings ogsIn = new OverrideGraphicSettings();
            OverrideGraphicSettings ogsOut = new OverrideGraphicSettings();
            ogsIn.SetProjectionLineColor( colorIn );
            ogsOut.SetProjectionLineColor( colorOut );

            //ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisY ), -elevationAngle );
        if ( typeName == "アウト") {
            document.ActiveView.SetElementOverrides( instance.Id, ogsIn );
            if ( conn.CoordinateSystem.BasisX.Y > 0 ) { 
                var rotationAngle = Math.Atan2( -direction.Y, direction.X );
                
                ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisZ ), rotationAngle );
            }
            else {
                var rotationAngle = Math.Atan2( direction.Y, direction.X );
                ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisZ ), rotationAngle );
            }
        } else if (typeName == "イン") {
            document.ActiveView.SetElementOverrides( instance.Id, ogsOut );
            if ( conn.CoordinateSystem.BasisX.Y > 0 ) {
                var rotationAngle = Math.Atan2( direction.Y, direction.X );
                ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisZ ), rotationAngle );
            }
            else {
                var rotationAngle = Math.Atan2( -direction.Y, direction.X );
                ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisZ ), rotationAngle );
            }
        }
        instance.SetProperty( RoutingParameter.RouteName, routeName );
            //instance.rotate()

        return instance;
    }

    public static FamilyInstance AddRackGuid( this Document document,  XYZ position )
    {
        var symbol = document.GetFamilySymbol( RoutingFamilyType.RackGuide )!;
        if ( false == symbol.IsActive )
            symbol.Activate();

        var instance = document.Create.NewFamilyInstance( position, symbol, StructuralType.NonStructural );

        return instance;
    }

    public static FamilyInstance AddCornPoint( this Document document, string routeName, XYZ position)
    {
        var symbol = document.GetFamilySymbol( RoutingFamilyType.CornPoint )!;
        if ( false == symbol.IsActive )
            symbol.Activate();

        var instance = document.Create.NewFamilyInstance( position, symbol, StructuralType.NonStructural );

        instance.SetProperty( RoutingParameter.RouteName, routeName );

        return instance;
    }

    #endregion

    #region Terminate Points

    public static FamilyInstance? FindTerminatePointElement( this Document document, int elementId )
    {
      var instance = document.GetElementById<FamilyInstance>( elementId ) ;
      if ( null == instance ) return null ;

      if ( instance.Symbol.Id != document.GetFamilySymbol( RoutingFamilyType.TerminatePoint )?.Id ) {
        // Family instance is not a pass point.
        return null ;
      }

      return instance ;
    }

    public static bool IsTerminatePoint( this Element element )
    {
      return element is FamilyInstance fi && fi.IsTerminatePoint() ;
    }

    public static bool IsTerminatePoint( this FamilyInstance element )
    {
      return element.IsRoutingFamilyInstanceOf( RoutingFamilyType.TerminatePoint ) || element.HasParameter( RoutingParameter.RelatedTerminatePointId ) ;
    }

    public static int? GetTerminatePointId( this Element element )
    {
      if ( element is not FamilyInstance fi ) return null ;

      if ( fi.IsRoutingFamilyInstanceOf( RoutingFamilyType.TerminatePoint ) ) return fi.Id.IntegerValue ;
      if ( element.TryGetProperty( RoutingParameter.RelatedTerminatePointId, out int id ) ) return id ;
      return null ;
    }

    public static FamilyInstance AddTerminatePoint( this Document document, string routeName, XYZ position, XYZ direction, double? radius )
    {
      var symbol = document.GetFamilySymbol( RoutingFamilyType.TerminatePoint )! ;
      if ( false == symbol.IsActive ) symbol.Activate() ;

      var instance = document.Create.NewFamilyInstance( position, symbol, StructuralType.NonStructural ) ;
      instance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( 0.0 ) ;
      if ( radius.HasValue ) {
        instance.LookupParameter( "Arent-RoundDuct-Diameter" ).Set( radius.Value * 2.0 ) ;
      }

      var elevationAngle = Math.Atan2( direction.Z, Math.Sqrt( direction.X * direction.X + direction.Y * direction.Y ) ) ;
      var rotationAngle = Math.Atan2( direction.Y, direction.X ) ;

      ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisY ), -elevationAngle ) ;
      ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisZ ), rotationAngle ) ;

      instance.SetProperty( RoutingParameter.RouteName, routeName ) ;
      
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

    public static IEnumerable<FamilyInstance> GetAllElementsOfPassPoint( this Document document, int passPointId )
    {
      var parameterName = document.GetParameterName( RoutingParameter.RelatedPassPointId ) ;
      if ( null == parameterName ) yield break ;

      var elm = document.GetElementById<FamilyInstance>( passPointId ) ;
      if ( null == elm ) yield break ;
      if ( elm.IsRoutingFamilyInstanceOf( RoutingFamilyType.PassPoint ) ) yield return elm ;

      var filter = new ElementParameterFilter( ParameterFilterRuleFactory.CreateSharedParameterApplicableRule( parameterName ) ) ;

      foreach ( var e in document.GetAllElements<Element>().OfCategory( RoutingBuiltInCategories ).OfNotElementType().Where( filter ).OfType<FamilyInstance>() ) {
        if ( e.IsRoutingFamilyInstanceOf( RoutingFamilyType.PassPoint ) ) continue ;
        if ( e.TryGetProperty( RoutingParameter.RelatedPassPointId, out int id ) && id == passPointId ) yield return e ;
      }
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
      return document.GetAllElementsOfRouteName<MEPCurve>( routeName ).SelectMany( e => e.GetRoutingConnectors( fromConnector ) ).Distinct() ;
    }
    public static (IReadOnlyCollection<Connector> From, IReadOnlyCollection<Connector>To) GetConnectors( this Document document, string routeName )
    {
      var fromList = document.CollectRoutingEndPointConnectors( routeName, true ).EnumerateAll() ;
      var toList = document.CollectRoutingEndPointConnectors( routeName, false ).EnumerateAll() ;
      return ( From: fromList, To: toList ) ;
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

    public static IEnumerable<IEndPoint> GetNearestEndPoints( this Element element, bool isFrom )
    {
      if ( false == element.TryGetProperty( isFrom ? RoutingParameter.NearestFromSideEndPoints : RoutingParameter.NearestToSideEndPoints, out string? str ) ) {
        return Array.Empty<IEndPoint>() ;
      }
      if ( null == str ) {
        return Array.Empty<IEndPoint>() ;
      }

      return EndPointExtensions.ParseEndPoints( element.Document, str ) ;
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