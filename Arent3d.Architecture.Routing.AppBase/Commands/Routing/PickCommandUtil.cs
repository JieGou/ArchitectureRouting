using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public static class PickCommandUtil
  {
    private static readonly double MinToleranceOfConnector = ( 350.0 ).MillimetersToRevitUnits();
    public static IDisposable SetTempColor( this UIDocument uiDocument, ConnectorPicker.IPickResult pickResult )
    {
      return new TempColorWrapper( uiDocument, pickResult.GetAllRelatedElements() ) ;
    }

    private class TempColorWrapper : MustBeDisposed
    {
      private readonly Document _document ;
      private readonly TempColor _tempColor ;
      public TempColorWrapper( UIDocument uiDocument, IEnumerable<ElementId> elements )
      {
        _document = uiDocument.Document ;
        _tempColor = new TempColor( uiDocument.ActiveView, new Color( 0, 0, 255 ) ) ;
        _document.Transaction( "TransactionName.Commands.Routing.Common.ChangeColor".GetAppStringByKeyOrDefault( null ), t =>
        {
          _tempColor.AddRange( elements ) ;
          return Result.Succeeded ;
        } ) ;
      }

      protected override void Finally()
      {
        _document.Transaction( "TransactionName.Commands.Routing.Common.RevertColor".GetAppStringByKeyOrDefault( null ), t =>
        {
          _tempColor.Dispose() ;
          return Result.Succeeded ;
        } ) ;
      }
    }

    public static IEndPoint GetEndPoint( ConnectorPicker.IPickResult pickResult, ConnectorPicker.IPickResult anotherResult, bool useConnectorDiameter )
    {
      var preferredRadius = ( pickResult.PickedConnector ?? anotherResult.PickedConnector )?.Radius ;
      if ( pickResult.PickedConnector is { } connector ) return new ConnectorEndPoint( connector, useConnectorDiameter ? null : preferredRadius ) ;

      var element = pickResult.PickedElement ;
      var pos = pickResult.GetOrigin() ;
      var anotherPos = anotherResult.GetOrigin() ;
      var dir = GetPreferredDirection( pos, anotherPos ) ;

      return new TerminatePointEndPoint( element.Document, string.Empty, pos, dir, preferredRadius, element.UniqueId ) ;
    }
    
    public static IEndPoint GetEndPoint( ConnectorPicker.IPickResult pickResult, FamilyInstance endPoint, bool useConnectorDiameter )
    {
      var preferredRadius = pickResult.PickedConnector?.Radius ;
      if ( pickResult.PickedConnector is { } connector ) return new ConnectorEndPoint( connector, useConnectorDiameter ? null : preferredRadius ) ;

      var element = pickResult.PickedElement ;
      var pos = pickResult.GetOrigin() ; 
      var point = ( endPoint.Location as LocationPoint )!.Point ;
      var dir = GetPreferredDirection( pos, point) ;

      return new TerminatePointEndPoint( element.Document, string.Empty, pos, dir, preferredRadius, element.UniqueId ) ;
    }

    public static IEndPoint GetEndPoint( FamilyInstance endPoint, ConnectorPicker.IPickResult pickResult )
    {
      var preferredRadius = pickResult.PickedConnector?.Radius ; 

      var element = pickResult.PickedElement ;
      var pos = ( endPoint.Location as LocationPoint )!.Point ; 
      var point = pickResult.GetOrigin() ;
      var dir = GetPreferredDirection( pos, point) ;

      return new TerminatePointEndPoint( element.Document, string.Empty, pos, dir, preferredRadius, endPoint.UniqueId ) ;
    }

    public static IEndPoint GetEndPoint( Document document, FamilyInstance startPoint, FamilyInstance endPoint, double preferredRadius )
    { 
      var pos = ( startPoint.Location as LocationPoint )!.Point ; 
      var point = ( endPoint.Location as LocationPoint )!.Point ; 
      var dir = GetPreferredDirection( pos,point) ;

      return new TerminatePointEndPoint( document, string.Empty, pos, dir, preferredRadius, startPoint.UniqueId ) ;
    }


    private static XYZ GetPreferredDirection( XYZ pos, XYZ anotherPos )
    {
      var dir = anotherPos - pos ;

      double x = Math.Abs( dir.X ), y = Math.Abs( dir.Y ) ;
      if ( x < y ) {
        return ( 0 <= dir.Y ) ? XYZ.BasisY : -XYZ.BasisY ;
      }
      else {
        return ( 0 <= dir.X ) ? XYZ.BasisX : -XYZ.BasisX ;
      }
    }

    public static (ConnectorPicker.IPickResult PickResult, bool AnotherIsFrom) PickResultFromAnother( Route route, IEndPoint endPoint )
    {
      var ((subRoute, anotherEndPoint), isFrom) = GetOtherEndPoint( route, endPoint ) ;
      return (new PseudoPickResult( subRoute, anotherEndPoint, isFrom ), isFrom) ;
    }

    public static IEndPoint CreateRouteEndPoint( ConnectorPicker.IPickResult routePickResult )
    {
      return new RouteEndPoint( routePickResult.SubRoute! ) ;
    }

    public static (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateBranchingRouteEndPoint( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, IFittingSizeCalculator fittingSizeCalculator, bool isFrom )
    {
      var element = routePickResult.GetOriginElement() ;
      var document = element.Document ;
      var subRoute = GetRepresentativeSubRoute( element ) ?? routePickResult.SubRoute! ;
      var pos = GetAdjustedOrigin( document, routePickResult, anotherPickResult, routeProperty, classificationInfo, fittingSizeCalculator, isFrom ) ;

      // Create Pass Point
      var routeName = subRoute.Route.Name ;
      if ( InsertBranchingPassPointElement( document, subRoute, element, pos ) is not { } passPointElement ) throw new InvalidOperationException() ;
      if ( isFrom ) {
        var fromEndPointKey = subRoute.FromEndPoints.First().Key ;
        var fromElementId = fromEndPointKey.GetElementUniqueId() ;
        var endPointKey = subRoute.ToEndPoints.First().Key ;
        var elementId = endPointKey.GetElementUniqueId() ;
        passPointElement.SetProperty( PassPointParameter.RelatedConnectorUniqueId, elementId ) ;
        passPointElement.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, fromElementId ) ;
      }
      var otherSegments = GetNewSegmentList( subRoute, element, passPointElement ).Select( segment => ( routeName, segment ) ).EnumerateAll() ;

      // Create PassPointBranchEndPoint
      var preferredRadius = ( routePickResult.PickedConnector ?? anotherPickResult.PickedConnector )?.Radius ;
      var endPoint = new PassPointBranchEndPoint( document, passPointElement.UniqueId, preferredRadius, routePickResult.EndPointOverSubRoute! ) ;

      return ( endPoint, otherSegments ) ;
    }

    private static SubRoute? GetRepresentativeSubRoute( Element element )
    {
      if ( ( element.GetRepresentativeSubRoute() ?? element.GetSubRouteInfo() ) is not { } subRouteInfo ) return null ;

      return RouteCache.Get( DocumentKey.Get( element.Document ) ).GetSubRoute( subRouteInfo ) ;
    }

    private static XYZ GetAdjustedOrigin( Document document, ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, IFittingSizeCalculator fittingSizeCalculator, bool isFrom )
    {
      var subRoutePos = routePickResult.GetOrigin() ;
      if ( routePickResult.PickedElement is not MEPCurve routeMepCurve ) return subRoutePos ; // Not a modification target 

      var connectorPos = anotherPickResult.GetOrigin() ;
      if ( anotherPickResult.GetMEPCurveDirection( false == isFrom ) is not { } connectorDir ) return subRoutePos ; // Not a modification target

      var (curveCommonSidePos, curveAnotherSidePos) = GetConnectorPositions( routeMepCurve, isFrom ) ;
      if ( null == curveCommonSidePos || null == curveAnotherSidePos || curveCommonSidePos.IsAlmostEqualTo( curveAnotherSidePos ) ) return subRoutePos ;  // Bad connector information
      if ( connectorDir.IsZeroLength() ) {
        // Guess connector direction from curve direction
        connectorDir = GetPreferredDirection( connectorPos, subRoutePos ) ;
      }
      else {
        connectorDir = connectorDir.Normalize() ;
      }

      var routeDir = curveAnotherSidePos - curveCommonSidePos ;
      var curveLength = routeDir.GetLength() ;
      var minimumLength = document.Application.ShortCurveTolerance ;
      if ( curveLength <= minimumLength * 2 ) return subRoutePos ; // Cannot modify: too short to modify
      routeDir /= curveLength ;
      if ( false == connectorDir.IsPerpendicularTo( routeDir, document.Application.AngleTolerance ) ) return subRoutePos ;  // Cannot modify: not perpendicular

      var mepSystem = new RouteMEPSystem( document, routeProperty.GetSystemType(), routeProperty.GetCurveType() ) ;
      var edgeDiameter = routeProperty.GetDiameter() ;
      var spec = new MEPSystemPipeSpec( mepSystem, fittingSizeCalculator ) ;
      var elbowSize = spec.GetLongElbowSize( edgeDiameter.DiameterValueToPipeDiameter() ) ;

      var pickPointDir = subRoutePos - connectorPos ;
      var distance = connectorDir.DotProduct( pickPointDir ) ;
      if ( distance <= elbowSize ) return subRoutePos ; // Cannot modify: not distant enough

      var adjustedPosParam = routeDir.DotProduct( connectorPos - curveCommonSidePos ) - ( elbowSize + minimumLength ) ;
      if ( curveLength - minimumLength <= adjustedPosParam ) {
        // Cannot modify: out of range
        // TODO: seek connecting conduit
        adjustedPosParam = curveLength - minimumLength ;
      }
      if ( adjustedPosParam <= minimumLength ) {
        // Cannot modify: out of range
        // TODO: seek connecting conduit
        return subRoutePos ;
      }

      return curveCommonSidePos + adjustedPosParam * routeDir ;

      static (XYZ? CommonSide, XYZ? AnotherSide) GetConnectorPositions( MEPCurve mepCurve, bool isFrom )
      {
        if ( mepCurve.GetRoutingConnectors( isFrom ).UniqueOrDefault() is not { } commonConnector ) return ( null, null ) ;
        if ( mepCurve.GetRoutingConnectors( false == isFrom ).UniqueOrDefault() is not { } anotherConnector ) return ( null, null ) ;
        return ( commonConnector.Origin, anotherConnector.Origin ) ;
      }
    }

    private static Instance? InsertBranchingPassPointElement( Document document, SubRoute subRoute, Element routingElement, XYZ pos )
    {
      if ( routingElement.GetRoutingConnectors( true ).FirstOrDefault() is not { } fromConnector ) return null ;
      if ( routingElement.GetRoutingConnectors( false ).FirstOrDefault() is not { } toConnector ) return null ;

      var dir = ( toConnector.Origin - fromConnector.Origin ).Normalize() ;
      return document.AddPassPoint( subRoute.Route.RouteName, pos, dir, subRoute.GetDiameter() * 0.5, routingElement.GetLevelId() ) ;
    }

    private const double HalfPI = Math.PI / 2 ;
    private const double OneAndAHalfPI = Math.PI + HalfPI ;
    private static double GetPreferredAngle( Transform transform, XYZ pos, XYZ anotherPos )
    {
      var vec = pos - anotherPos ;
      var x = transform.BasisY.DotProduct( vec ) ;
      var y = transform.BasisZ.DotProduct( vec ) ;
      if ( Math.Abs( y ) < Math.Abs( x ) ) {
        return ( 0 < x ? 0 : Math.PI ) ;
      }
      else {
        return ( 0 < y ? HalfPI : OneAndAHalfPI ) ;
      }
    }

    public static IEnumerable<RouteSegment> GetNewSegmentList( SubRoute subRoute, Element insertingElement, Instance passPointElement )
    {
      var detector = new RouteSegmentDetector( subRoute, insertingElement ) ;
      var passPoint = new PassPointEndPoint( passPointElement ) ;
      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( detector.IsPassingThrough( segment ) ) {
          // split segment
          var diameter = segment.GetRealNominalDiameter() ?? segment.PreferredNominalDiameter ;
          var isRoutingOnPipeSpace = segment.IsRoutingOnPipeSpace ;
          var fromFixedHeight = segment.FromFixedHeight ;
          var toFixedHeight = segment.ToFixedHeight ;
          var avoidType = segment.AvoidType ;
          var shaft1 = ( segment.FromEndPoint.GetLevelId( subRoute.Route.Document ) != passPoint.GetLevelId( subRoute.Route.Document ) ) ? segment.ShaftElementUniqueId : null ;
          var shaft2 = ( passPoint.GetLevelId( subRoute.Route.Document ) != segment.ToEndPoint.GetLevelId( subRoute.Route.Document ) ) ? segment.ShaftElementUniqueId : null ;
          yield return new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, passPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaft1 ) ;
          yield return new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, passPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaft2 ) ;
        }
        else {
          yield return segment ;
        }
      }
    }

    private static ((SubRoute SubRoute, IEndPoint EndPoint), bool IsFrom) GetOtherEndPoint( Route route, IEndPoint endPoint )
    {
      var endPointSubRouteMap = new Dictionary<IEndPoint, (SubRoute? OfFrom, SubRoute? OfTo)>() ;
      foreach ( var subRoute in route.SubRoutes ) {
        // from-side
        foreach ( var fromEndPoint in subRoute.FromEndPoints ) {
          if ( endPointSubRouteMap.TryGetValue( fromEndPoint, out var tuple ) ) {
            endPointSubRouteMap[ fromEndPoint ] = ( subRoute, tuple.OfTo ) ;
          }
          else {
            endPointSubRouteMap.Add( fromEndPoint, ( subRoute, null ) ) ;
          }
        }

        // to-side
        foreach ( var toEndPoint in subRoute.ToEndPoints ) {
          if ( endPointSubRouteMap.TryGetValue( toEndPoint, out var tuple ) ) {
            endPointSubRouteMap[ toEndPoint ] = ( tuple.OfFrom, subRoute ) ;
          }
          else {
            endPointSubRouteMap.Add( toEndPoint, ( null, subRoute ) ) ;
          }
        }
      }

      // seek other end point
      if ( false == endPointSubRouteMap.TryGetValue( endPoint, out var ofFromTo ) ) throw new InvalidOperationException() ;

      if ( null != ofFromTo.OfFrom ) {
        return (TrailFrom( endPointSubRouteMap, ofFromTo.OfFrom ) ?? throw new InvalidOperationException(), true) ;
      }
      else {
        return (TrailTo( endPointSubRouteMap, ofFromTo.OfTo! ) ?? throw new InvalidOperationException(), false) ;
      }
    }

    private static (SubRoute, IEndPoint)? TrailFrom( Dictionary<IEndPoint, (SubRoute? OfFrom, SubRoute? OfTo)> endPointSubRouteMap, SubRoute subRoute )
    {
      foreach ( var toEndPoint in subRoute.ToEndPoints ) {
        if ( false == endPointSubRouteMap.TryGetValue( toEndPoint, out var tuple ) ) continue ;

        if ( null == tuple.OfFrom ) return ( subRoute, toEndPoint ) ;
        if ( TrailFrom( endPointSubRouteMap, tuple.OfTo! ) is { } result ) return result ;
      }

      return null ;
    }

    private static (SubRoute, IEndPoint)? TrailTo( Dictionary<IEndPoint, (SubRoute? OfFrom, SubRoute? OfTo)> endPointSubRouteMap, SubRoute subRoute )
    {
      foreach ( var fromEndPoint in subRoute.FromEndPoints ) {
        if ( false == endPointSubRouteMap.TryGetValue( fromEndPoint, out var tuple ) ) continue ;

        if ( null == tuple.OfTo ) return ( subRoute, fromEndPoint ) ;
        if ( TrailTo( endPointSubRouteMap, tuple.OfFrom! ) is { } result ) return result ;
      }

      return null ;
    }

    #region Preview Lines

    public static ( List<ElementId>, List<ElementId> ) CreatePreviewLines( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      if ( fromPickResult.PickedConnector == null || toPickResult.PickedConnector == null ) {
        return ( new List<ElementId>(), new List<ElementId>() ) ;
      }
      using Transaction trans = new( document, "Create preview lines" ) ;
      trans.Start() ;
      var firstPoint = fromPickResult.GetOrigin() ;
      var lastPoint = toPickResult.GetOrigin() ;
      if ( Math.Abs( firstPoint.X - lastPoint.X ) < MinToleranceOfConnector || Math.Abs( firstPoint.Y - lastPoint.Y ) < MinToleranceOfConnector ) 
        return ( new List<ElementId>(), new List<ElementId>() ) ;
      
      var (secondPoint, thirdPoint, isDirectionXOrY ) = GetPoints( fromPickResult, toPickResult ) ;
      
      List<ElementId> previewLineIds = new() ;
      List<ElementId> allLineIds = new() ;
      var redColor = new Color( 255, 0, 0 ) ;
      var blueColor = new Color( 0, 0, 255 ) ;
      var redLineCategory = GetLineStyle( document, redColor, "Red" ) ;
      var blueLineCategory = GetLineStyle( document, blueColor, "Blue" ) ;

      CreateLines( document, redLineCategory, isDirectionXOrY, firstPoint, lastPoint, secondPoint, allLineIds, previewLineIds ) ;
      CreateLines( document, blueLineCategory, isDirectionXOrY, firstPoint, lastPoint, thirdPoint, allLineIds, previewLineIds ) ;

      trans.Commit() ;

      return ( previewLineIds, allLineIds ) ;
    }

    private static ( XYZ, XYZ, bool ) GetPoints( ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      const double tolerance = 0.001 ;
      var toOrigin = toPickResult.GetOrigin() ;
      var fromOrigin = fromPickResult.GetOrigin() ;
      var origin = new Vector3d( fromOrigin.X, fromOrigin.Y, fromOrigin.Z ) ;
      var direction = XYZ.BasisX ;
      var toDirection = -XYZ.BasisX ;

      Vector3d secondDirection ;
      Vector3d firstPoint, secondPoint ;
      var firstDirection = new Vector3d( fromOrigin.X < toOrigin.X ? Math.Abs( direction.X ) : -Math.Abs( direction.X ), fromOrigin.Y < toOrigin.Y ? Math.Abs( direction.Y ) : -Math.Abs( direction.Y ), direction.Z ) ;
      if ( firstDirection.x != 0 ) {
        var y = firstDirection.x is 1 or -1 ? ( fromOrigin.Y < toOrigin.Y ? Math.Abs( direction.X ) : -Math.Abs( direction.X ) ) : ( fromOrigin.Y > toOrigin.Y ? Math.Abs( direction.X ) : -Math.Abs( direction.X ) ) ;
        var x = Math.Abs( y - firstDirection.x ) == 0 ? -firstDirection.y : firstDirection.y ;
        secondDirection = new Vector3d( x, y, firstDirection.z ) ;
      }
      else {
        var x = firstDirection.y is 1 or -1 ? ( fromOrigin.X < toOrigin.X ? Math.Abs( direction.Y ) : -Math.Abs( direction.Y ) ) : ( fromOrigin.X > toOrigin.X ? Math.Abs( direction.Y ) : -Math.Abs( direction.Y ) ) ;
        var y = Math.Abs( x - firstDirection.y ) == 0 ? -firstDirection.x : firstDirection.x ;
        secondDirection = new Vector3d( x, y, firstDirection.z ) ;
      }

      var isDirectionXOrY = ( ( direction.X is 1 or -1 && Math.Abs( direction.Y ) < tolerance ) || ( direction.Y is 1 or -1 && Math.Abs( direction.X ) < tolerance ) )
                            && ( ( toDirection.X is 1 or -1 && Math.Abs( toDirection.Y ) < tolerance ) || ( toDirection.Y is 1 or -1 && Math.Abs( toDirection.X ) < tolerance ) ) ;
      var firstLine = new MathLib.Line( origin, firstDirection ) ;
      var secondLine = new MathLib.Line( origin, secondDirection ) ;
      var xLength = Math.Abs( fromOrigin.X - toOrigin.X ) ;
      var yLength = Math.Abs( fromOrigin.Y - toOrigin.Y ) ;
      if ( isDirectionXOrY ) {
        firstPoint = firstLine.GetPointAt( firstDirection.x is 1 or -1 ? xLength : yLength ) ;
        secondPoint = secondLine.GetPointAt( secondDirection.x is 1 or -1 ? xLength : yLength ) ;
      }
      else {
        var length = Math.Min( xLength, yLength ) ;
        firstPoint = firstLine.GetPointAt( length ) ;
        secondPoint = secondLine.GetPointAt( length ) ;
      }

      return ( new XYZ( firstPoint.x, firstPoint.y, firstPoint.z ), new XYZ( secondPoint.x, secondPoint.y, secondPoint.z ), isDirectionXOrY ) ;
    }

    private static void CreateLines( Document document, Category lineCategory, bool isDirectionXOrY, XYZ firstPoint, XYZ lastPoint, XYZ secondPoint, ICollection<ElementId> allLineIds, ICollection<ElementId> previewLineIds )
    {
      const double lengthLine = 0.5 ;
      var curve = Line.CreateBound( firstPoint, secondPoint ) ;
      var detailCurve = document.Create.NewDetailCurve( document.ActiveView, curve ) ;
      detailCurve.LineStyle = lineCategory.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      previewLineIds.Add( detailCurve.Id ) ;
      allLineIds.Add( detailCurve.Id ) ;

      if ( isDirectionXOrY ) {
        lastPoint = new XYZ( lastPoint.X, lastPoint.Y, firstPoint.Z ) ;
        curve = Line.CreateBound( secondPoint, lastPoint ) ;
        detailCurve = document.Create.NewDetailCurve( document.ActiveView, curve ) ;
        detailCurve.LineStyle = lineCategory.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
        previewLineIds.Add( detailCurve.Id ) ;
        allLineIds.Add( detailCurve.Id ) ;
      }
      else {
        var (x, y, z) = curve.Origin ;
        var (a, b, c) = curve.Direction ;
        var mainLine = new MathLib.Line( new Vector3d( x, y, z ), new Vector3d( a, b, c ) ) ;
        var length = curve.Length ;
        var origin = mainLine.GetPointAt( length - lengthLine ) ;
        var direction = new Vector3d( b, -a, c ) ;
        var firstLine = new MathLib.Line( origin, direction ) ;
        var (x1, y1, z1) = firstLine.GetPointAt( lengthLine ) ;
        var (x2, y2, z2) = firstLine.GetPointAt( -lengthLine ) ;
      
        curve = Line.CreateBound( secondPoint, new XYZ( x1, y1, z1 ) ) ;
        detailCurve = document.Create.NewDetailCurve( document.ActiveView, curve ) ;
        detailCurve.LineStyle = lineCategory.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
        allLineIds.Add( detailCurve.Id ) ;
      
        curve = Line.CreateBound( secondPoint, new XYZ( x2, y2, z2 ) ) ;
        detailCurve = document.Create.NewDetailCurve( document.ActiveView, curve ) ;
        detailCurve.LineStyle = lineCategory.GetGraphicsStyle( GraphicsStyleType.Projection ) ;
        allLineIds.Add( detailCurve.Id ) ;
      }
    }

    public static void RemovePreviewLines( Document document, List<ElementId> previewLineIds )
    {
      using Transaction trans = new( document, "Remove preview lines" ) ;
      trans.Start() ;
      document.Delete( previewLineIds ) ;
      trans.Commit() ;
    }
    
    private static Category GetLineStyle( Document doc, Color color, string colorName )
    {
      var categories = doc.Settings.Categories ;
      var subCategoryName = colorName + "PreviewLine" ;
      Category category = doc.Settings.Categories.get_Item( BuiltInCategory.OST_GenericAnnotation ) ;
      Category subCategory ;
      if ( ! category.SubCategories.Contains( subCategoryName ) ) {
        subCategory = categories.NewSubcategory( category, subCategoryName ) ;
        subCategory.LineColor = color ;
      }
      else
        subCategory = category.SubCategories.get_Item( subCategoryName ) ;

      return subCategory ;
    }
    
    #endregion

    private class PseudoPickResult : ConnectorPicker.IPickResult
    {
      private readonly SubRoute _subRoute ;
      private readonly IEndPoint _endPoint ;

      public PseudoPickResult( SubRoute subRoute, IEndPoint endPoint, bool isFrom )
      {
        _subRoute = subRoute ;
        _endPoint = endPoint ;

        if ( endPoint is RouteEndPoint routeEndPoint ) {
          SubRoute = routeEndPoint.ParentSubRoute() ?? subRoute ;
          EndPointOverSubRoute = null ;
        }
        else if ( endPoint is PassPointBranchEndPoint passPointBranchEndPoint ) {
          SubRoute = passPointBranchEndPoint.GetSubRoute( isFrom ) ?? subRoute ;
          EndPointOverSubRoute = passPointBranchEndPoint.EndPointKeyOverSubRoute ;
        }
        else {
          SubRoute = null ;
          EndPointOverSubRoute = null ;
        }
      }

      public IEnumerable<ElementId> GetAllRelatedElements() => Enumerable.Empty<ElementId>() ;
      public ElementId GetLevelId() => _endPoint.GetLevelId( _subRoute.Route.Document ) ;

      public SubRoute? SubRoute { get ; }
      public EndPointKey? EndPointOverSubRoute { get ; }
      public Element PickedElement => throw new InvalidOperationException() ;
      public Connector? PickedConnector => ( _endPoint as ConnectorEndPoint )?.GetConnector() ?? _subRoute.GetReferenceConnector() ;
      public XYZ GetOrigin() => _endPoint.RoutingStartPosition ;
      public XYZ? GetMEPCurveDirection( bool isFrom ) => _endPoint.GetRoutingDirection( isFrom ) * ( isFrom ? 1.0 : -1.0 ) ;
      public Element GetOriginElement() => throw new InvalidOperationException() ;

      public bool IsCompatibleTo( Connector connector )
      {
        return ( PickedConnector ?? SubRoute?.GetReferenceConnector() ?? _subRoute.GetReferenceConnector() ).IsCompatibleTo( connector ) ;
      }

      public bool IsCompatibleTo( Element element )
      {
        return ( _subRoute.Route.RouteName != element.GetRouteName() ) ;
      }
    }
  }
}