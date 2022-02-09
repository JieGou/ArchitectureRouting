using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class SelectionRangeRouteCommandBase : RoutingCommandBase<SelectionRangeRouteCommandBase.SelectState>
  {
    private const string ErrorMessageNoPowerAndSensorConnector = "No power connectors and sensor connectors are selected." ;
    private const string ErrorMessageNoPowerConnector = "No power connectors are selected." ;
    private const string ErrorMessageTwoOrMorePowerConnector = "Two or more power connectors are selected." ;
    private const string ErrorMessageNoSensorConnector = "No sensor connectors are selected on the power connector level." ;
    private const string ErrorMessageSensorConnector = "At least two sensor connectors on the power connector level must be selected." ;
    private const string ErrorMessageCannotDetermineSensorConnectorArrayDirection = "Couldn't determine sensor array direction" ;

    public enum SensorArrayDirection
    {
      Invalid,
      XMinus,
      YMinus,
      XPlus,
      YPlus,
    }

    private static readonly double DefaultFootLengthMeters = 1.5.MetersToRevitUnits() ;
    private const double DefaultFootLengthFeet = 5.0 ;

    private static double GetFootLength( DisplayUnit displayUnitSystem ) =>
      displayUnitSystem switch
      {
        DisplayUnit.METRIC => DefaultFootLengthMeters,
        DisplayUnit.IMPERIAL => DefaultFootLengthFeet,
        _ => throw new ArgumentOutOfRangeException( nameof( displayUnitSystem ), displayUnitSystem, null ),
      } ;

    public record SelectState( FamilyInstance PowerConnector, IReadOnlyList<FamilyInstance> SensorConnectors, SensorArrayDirection SensorDirection, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo, MEPSystemPipeSpec PipeSpec ) ;

    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;

    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;

    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;

    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected abstract (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom ) ;

    protected override OperationResult<SelectState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var routingExecutor = GetRoutingExecutor() ;

      var (powerConnector, sensorConnectors, sensorDirection, errorMessage ) = SelectionRangeRoute( uiDocument ) ;
      if ( null != errorMessage ) return OperationResult<SelectState>.FailWithMessage( errorMessage ) ;

      var farthestSensorConnector = sensorConnectors.Last() ;
      var property = ShowPropertyDialog( uiDocument.Document, powerConnector!, farthestSensorConnector ) ;
      if ( true != property?.DialogResult ) return OperationResult<SelectState>.Cancelled ;

      if ( GetMEPSystemClassificationInfo( powerConnector!, farthestSensorConnector, property.GetSystemType() ) is not { } classificationInfo ) return OperationResult<SelectState>.Failed ;

      var pipeSpec = new MEPSystemPipeSpec( new RouteMEPSystem( uiDocument.Document, property.GetSystemType(), property.GetCurveType() ), routingExecutor.FittingSizeCalculator ) ;

      return new OperationResult<SelectState>( new SelectState( powerConnector!, sensorConnectors, sensorDirection, property, classificationInfo, pipeSpec ) ) ;
    }

    private static ( FamilyInstance? PowerConnector, IReadOnlyList<FamilyInstance> SensorConnectors, SensorArrayDirection SensorDirection, string? ErrorMessage ) SelectionRangeRoute( UIDocument iuDocument )
    {
      var selectedElements = iuDocument.Selection.PickElementsByRectangle( ConnectorFamilySelectionFilter.Instance, "ドラックで複数コネクタを選択して下さい。" ).OfType<FamilyInstance>() ;

      FamilyInstance? powerConnector = null ;
      var sensorConnectors = new List<FamilyInstance>() ;
      foreach ( var element in selectedElements ) {
        if ( element.GetConnectorFamilyType() is not { } connectorFamilyType ) continue ;

        if ( connectorFamilyType == ConnectorFamilyType.Power ) {
          if ( null != powerConnector ) return ( null!, Array.Empty<FamilyInstance>(), SensorArrayDirection.Invalid, ErrorMessageTwoOrMorePowerConnector ) ;
          powerConnector = element ;
        }
        else if ( connectorFamilyType == ConnectorFamilyType.Sensor ) {
          sensorConnectors.Add( element ) ;
        }
      }

      if ( powerConnector == null && 0 == sensorConnectors.Count ) return ( null, Array.Empty<FamilyInstance>(), default, ErrorMessageNoPowerAndSensorConnector ) ;
      if ( powerConnector == null ) return ( null, Array.Empty<FamilyInstance>(), SensorArrayDirection.Invalid, ErrorMessageNoPowerConnector ) ;

      var powerLevel = powerConnector.LevelId ;
      sensorConnectors.RemoveAll( fi => fi.LevelId != powerLevel ) ;

      if ( 0 == sensorConnectors.Count ) return ( null, Array.Empty<FamilyInstance>(), SensorArrayDirection.Invalid, ErrorMessageNoSensorConnector ) ;
      if ( 1 == sensorConnectors.Count ) return ( null, Array.Empty<FamilyInstance>(), SensorArrayDirection.Invalid, ErrorMessageSensorConnector ) ;

      var sensorDirection = SortSensorConnectors( powerConnector, sensorConnectors ) ;
      if ( SensorArrayDirection.Invalid == sensorDirection ) return ( null, Array.Empty<FamilyInstance>(), SensorArrayDirection.Invalid, ErrorMessageCannotDetermineSensorConnectorArrayDirection ) ;

      return ( powerConnector, sensorConnectors, sensorDirection, null ) ;
    }

    private static SensorArrayDirection SortSensorConnectors( FamilyInstance powerConnector, List<FamilyInstance> sensorConnectors )
    {
      var powerPoint = powerConnector.GetTopConnectorOfConnectorFamily().Origin ;

      double minX = double.MaxValue, minY = double.MaxValue ;
      double maxX = -double.MaxValue, maxY = -double.MaxValue ;

      var sensorToOrigin = new Dictionary<FamilyInstance, XYZ>( sensorConnectors.Count ) ;
      foreach ( var sensor in sensorConnectors ) {
        var origin = sensor.GetTopConnectorOfConnectorFamily().Origin ;
        sensorToOrigin.Add( sensor, origin ) ;

        var (x, y, _) = origin ;
        if ( x < minX ) minX = x ;
        if ( y < minY ) minY = y ;
        if ( maxX < x ) maxX = x ;
        if ( maxY < y ) maxY = y ;
      }

      var (powerX, powerY, _) = powerPoint ;

      var xRange = GetRange( minX, maxX, powerX ) ;
      var yRange = GetRange( minY, maxY, powerY ) ;
      if ( xRange < 0 && yRange < 0 ) return SensorArrayDirection.Invalid ;

      SensorArrayDirection dir ;
      if ( yRange <= xRange ) {
        dir = ( maxX < powerX ) ? SensorArrayDirection.XMinus : SensorArrayDirection.XPlus ;
      }
      else {
        dir = ( maxY < powerY ) ? SensorArrayDirection.YMinus : SensorArrayDirection.YPlus ;
      }

      sensorConnectors.Sort( ( a, b ) => Compare( sensorToOrigin, a, b, dir ) ) ;
      return dir ;

      static double GetRange( double min, double max, double refPos )
      {
        if ( min <= refPos && refPos <= max ) return -1.0 ;  // cannot use
        return max - min ;
      }

      static int Compare( Dictionary<FamilyInstance, XYZ> sensorToOrigin, FamilyInstance a, FamilyInstance b, SensorArrayDirection dir ) =>
        dir switch
        {
          SensorArrayDirection.XMinus => sensorToOrigin[ b ].X.CompareTo( sensorToOrigin[ a ].X ),
          SensorArrayDirection.YMinus => sensorToOrigin[ b ].Y.CompareTo( sensorToOrigin[ a ].Y ),
          SensorArrayDirection.XPlus => sensorToOrigin[ a ].X.CompareTo( sensorToOrigin[ b ].X ),
          SensorArrayDirection.YPlus => sensorToOrigin[ a ].Y.CompareTo( sensorToOrigin[ b ].Y ),
          _ => throw new ArgumentOutOfRangeException( nameof( dir ), dir, null )
        } ;
    }

    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( Element fromPickElement, Element toPickElement, MEPSystemType? systemType )
    {
      if ( ( fromPickElement.GetConnectors().FirstOrDefault() ?? toPickElement.GetConnectors().FirstOrDefault() ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType( systemType ) ;
    }

    private RoutePropertyDialog? ShowPropertyDialog( Document document, Element fromPickElement, Element toPickElement )
    {
      var fromLevelId = fromPickElement.LevelId ;
      var toLevelId = toPickElement.LevelId ;

      if ( ( fromPickElement.GetConnectors().FirstOrDefault() ?? toPickElement.GetConnectors().FirstOrDefault() ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return ShowDialog( document, initValues, fromLevelId, toLevelId ) ;
      }

      return ShowDialog( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    private static RoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, initValues.ClassificationInfo, fromLevelId, toLevelId ) ;
      var sv = new RoutePropertyDialog( document, routeChoiceSpec, new RouteProperties( document, initValues.ClassificationInfo, initValues.SystemType, initValues.CurveType, routeChoiceSpec.StandardTypes?.FirstOrDefault(), initValues.Diameter ) ) ;

      sv.ShowDialog() ;

      return sv ;
    }

    private static RoutePropertyDialog ShowDialog( Document document, AddInType addInType, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, addInType, fromLevelId, toLevelId ) ;
      var sv = new RoutePropertyDialog( document, routeChoiceSpec, new RouteProperties( document, routeChoiceSpec ) ) ;
      sv.ShowDialog() ;

      return sv ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, SelectState selectState )
    {
      var (powerConnector, sensorConnectors, sensorDirection, routeProperty, classificationInfo, pipeSpec) = selectState ;

      var systemType = routeProperty.GetSystemType() ;
      var curveType = routeProperty.GetCurveType() ;
      var sensorFixedHeight = routeProperty.GetFromFixedHeight() ;
      var avoidType = routeProperty.GetAvoidType() ;
      var diameter = routeProperty.GetDiameter() ;
      var radius = diameter * 0.5 ;
      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( RouteCache.Get( DocumentKey.Get( document ) ), nameBase ) ;
      var routeName = nameBase + "_" + nextIndex ;

      var (footPassPoint, passPoints) = CreatePassPoints( routeName, powerConnector, sensorConnectors, sensorDirection, routeProperty, classificationInfo, pipeSpec ) ;
      document.Regenerate() ; // Apply Arent-RoundDuct-Diameter

      var result = new List<(string RouteName, RouteSegment Segment)>( passPoints.Count * 2 + 1 ) ;

      // main route
      var powerConnectorEndPoint = new ConnectorEndPoint( powerConnector.GetTopConnectorOfConnectorFamily(), radius ) ;
      var powerConnectorEndPointKey = powerConnectorEndPoint.Key ;
      {
        var secondFromEndPoints = EliminateSamePassPoints( footPassPoint, passPoints ).Select( pp => (IEndPoint)new PassPointEndPoint( pp ) ).ToList() ;
        var secondToEndPoints = secondFromEndPoints.Skip( 1 ).Append( new ConnectorEndPoint( sensorConnectors.Last().GetTopConnectorOfConnectorFamily(), radius ) ) ;
        var firstToEndPoint = secondFromEndPoints[ 0 ] ;

        result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, powerConnectorEndPoint, firstToEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
        result.AddRange( secondFromEndPoints.Zip( secondToEndPoints, ( f, t ) =>
        {
          var segment = new RouteSegment( classificationInfo, systemType, curveType, f, t, diameter, false, sensorFixedHeight, sensorFixedHeight, avoidType, null ) ;
          return ( routeName, segment ) ;
        } ) ) ;
      }

      // branch routes
      result.AddRange( passPoints.Zip( sensorConnectors.Take( passPoints.Count ), ( pp, sensor ) =>
      {
        var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
        var branchEndPoint = new PassPointBranchEndPoint( document, pp.UniqueId, radius, powerConnectorEndPointKey ) ;
        var connectorEndPoint = new ConnectorEndPoint( sensor.GetTopConnectorOfConnectorFamily(), radius ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, diameter, false, sensorFixedHeight, sensorFixedHeight, avoidType, null ) ;
        return ( subRouteName, segment ) ;
      } ) ) ;
      
      // change color connectors
      var allConnectors = new List<FamilyInstance> { powerConnector } ;
      allConnectors.AddRange( sensorConnectors ) ;
      var color = new Color( 0, 0, 0 ) ;
      ConfirmUnsetCommandBase.ChangeElementColor( document, allConnectors, color ) ;

      return result ;

      static IEnumerable<FamilyInstance> EliminateSamePassPoints( FamilyInstance? firstPassPoint, IEnumerable<FamilyInstance> passPoints )
      {
        if ( null != firstPassPoint ) yield return firstPassPoint ;

        var lastId = firstPassPoint?.Id ?? ElementId.InvalidElementId ;
        foreach ( var passPoint in passPoints ) {
          if ( passPoint.Id == lastId ) continue ;
          lastId = passPoint.Id ;
          yield return passPoint ;
        }
      }
    }

    private static (FamilyInstance? Foot, IReadOnlyList<FamilyInstance> Others) CreatePassPoints( string routeName, FamilyInstance powerConnector, IReadOnlyCollection<FamilyInstance> sensorConnectors, SensorArrayDirection sensorDirection, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, MEPSystemPipeSpec pipeSpec )
    {
      var document = powerConnector.Document ;
      var levelId = powerConnector.LevelId ;
      var diameter = routeProperty.GetDiameter() ;
      var bendingRadius = pipeSpec.GetLongElbowSize( diameter.DiameterValueToPipeDiameter() ) ;
      var forcedFixedHeight = PassPointEndPoint.GetForcedFixedHeight( document, routeProperty.GetFromFixedHeight(), levelId ) ;
      var sensorConnectorsWithoutLast = sensorConnectors.Take( sensorConnectors.Count - 1 ).ToReadOnlyCollection( sensorConnectors.Count - 1 ) ;
      var passPointPositions = GetPassPointPositions( powerConnector, sensorConnectorsWithoutLast, sensorConnectors.Last(), sensorDirection, forcedFixedHeight, bendingRadius ) ;

      var passPointDirection = sensorDirection switch
      {
        SensorArrayDirection.XMinus => -XYZ.BasisX,
        SensorArrayDirection.YMinus => -XYZ.BasisY,
        SensorArrayDirection.XPlus => XYZ.BasisX,
        SensorArrayDirection.YPlus => XYZ.BasisY,
        _ => throw new ArgumentOutOfRangeException( nameof( sensorDirection ), sensorDirection, null )
      } ;

      var passPoints = new List<FamilyInstance>( passPointPositions.Count + 1 ) ;
      var footPassPoint = CreateFootPassPoint( routeName, powerConnector, passPointPositions[ 0 ], sensorDirection, bendingRadius, diameter * 0.5, levelId ) ;
      footPassPoint!.SetProperty( PassPointParameter.RelatedConnectorUniqueId, sensorConnectors.Last().UniqueId ) ;
      footPassPoint!.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, powerConnector.UniqueId ) ;

      var lastPos = footPassPoint?.GetTotalTransform().Origin ;
      var lastFamilyInstance = footPassPoint ;
      var connectorIndex = 0 ;
      foreach ( var pos in passPointPositions ) {
        var connectorId = sensorConnectors.ElementAt( connectorIndex ).UniqueId ;
        if ( null != lastFamilyInstance && AreTooClose( lastPos!, pos, MEPSystemPipeSpec.MinimumShortCurveLength ) ) {
          // reuse last family instance
          lastFamilyInstance.SetProperty( PassPointParameter.RelatedConnectorUniqueId, connectorId ) ;
          lastFamilyInstance.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, powerConnector.UniqueId ) ;
          passPoints.Add( lastFamilyInstance ) ;
        }
        else {
          // create new family instance
          lastPos = pos ;
          var newFamilyInstance = document.AddPassPoint( routeName, pos, passPointDirection, diameter * 0.5, levelId ) ;
          newFamilyInstance.SetProperty( PassPointParameter.RelatedConnectorUniqueId, connectorId ) ;
          newFamilyInstance.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, powerConnector.UniqueId ) ;
          passPoints.Add( newFamilyInstance ) ;
          lastFamilyInstance = newFamilyInstance ;
        }

        connectorIndex++ ;
      }

      return ( footPassPoint, passPoints ) ;

      static bool AreTooClose( XYZ lastPos, XYZ nextPos, double shortCurveLength )
      {
        var manhattanDistance = Math.Abs( lastPos.X - nextPos.X ) + Math.Abs( lastPos.Y - nextPos.Y ) ;
        return ( manhattanDistance < shortCurveLength ) ;
      }

      static FamilyInstance? CreateFootPassPoint( string routeName, FamilyInstance powerConnector, XYZ firstPassPosition, SensorArrayDirection sensorDirection, double bendingRadius, double radius, ElementId levelId )
      {
        var document = powerConnector.Document ;
        var footLength = GetFootLength( document.DisplayUnitSystem ) ;
        var powerPosition = powerConnector.GetTopConnectorOfConnectorFamily().Origin ;

        var (sensorDirDistance, anotherDistance) = sensorDirection switch
        {
          SensorArrayDirection.XMinus => ( powerPosition.X - firstPassPosition.X, ( firstPassPosition.Y - powerPosition.Y ) ),
          SensorArrayDirection.YMinus => ( powerPosition.Y - firstPassPosition.Y, ( firstPassPosition.X - powerPosition.X ) ),
          SensorArrayDirection.XPlus => ( firstPassPosition.X - powerPosition.X, ( firstPassPosition.Y - powerPosition.Y ) ),
          SensorArrayDirection.YPlus => ( firstPassPosition.Y - powerPosition.Y, ( firstPassPosition.X - powerPosition.X ) ),
          _ => throw new ArgumentOutOfRangeException( nameof( sensorDirection ), sensorDirection, null ),
        } ;

        if ( sensorDirDistance <= bendingRadius * 2 ) return null ; // cannot insert foot pass point
        if ( Math.Abs( anotherDistance ) <= bendingRadius * 2 ) return null ; // cannot insert foot pass point
        var sensorDirPos = ( sensorDirDistance <= bendingRadius + footLength ) ? sensorDirDistance * 0.5 : footLength ;

        var (passPointPos, passPointDir) = sensorDirection switch
        {
          SensorArrayDirection.XMinus => (new XYZ( powerPosition.X - sensorDirPos, ( powerPosition.Y + firstPassPosition.Y ) * 0.5, firstPassPosition.Z ), XYZ.BasisY),
          SensorArrayDirection.YMinus => (new XYZ( ( powerPosition.X + firstPassPosition.X ) * 0.5, powerPosition.Y - sensorDirPos, firstPassPosition.Z ), XYZ.BasisX),
          SensorArrayDirection.XPlus => (new XYZ( powerPosition.X + sensorDirPos, ( powerPosition.Y + firstPassPosition.Y ) * 0.5, firstPassPosition.Z ), XYZ.BasisY),
          SensorArrayDirection.YPlus => (new XYZ( ( powerPosition.X + firstPassPosition.X ) * 0.5, powerPosition.Y + sensorDirPos, firstPassPosition.Z ), XYZ.BasisX),
          _ => throw new ArgumentOutOfRangeException( nameof( sensorDirection ), sensorDirection, null ),
        } ;
        passPointDir *= Math.Sign( anotherDistance ) ;

        return document.AddPassPoint( routeName, passPointPos, passPointDir, radius, levelId ) ;
      }

      static IReadOnlyList<XYZ> GetPassPointPositions( FamilyInstance powerConnector, IReadOnlyCollection<FamilyInstance> sensorConnectors, FamilyInstance lastSensorConnector, SensorArrayDirection sensorDirection, double? forcedFixedHeight, double bendingRadius )
      {
        var powerPosition = powerConnector.GetTopConnectorOfConnectorFamily().Origin ;
        var sensorPositions = sensorConnectors.ConvertAll( sensorConnector => sensorConnector.GetTopConnectorOfConnectorFamily().Origin ) ;
        var lastSensorPosition = lastSensorConnector.GetTopConnectorOfConnectorFamily().Origin ;

        var fixedHeight = forcedFixedHeight ?? GetPreferredRouteHeight( powerPosition, sensorPositions, lastSensorPosition, bendingRadius ) ;
        var (sensorLineX, sensorLineY, sensorLineZ) = GetSensorLine( powerPosition, sensorPositions, lastSensorPosition, sensorDirection, bendingRadius, fixedHeight ) ;

        var passPointOffset = bendingRadius + MEPSystemPipeSpec.MinimumShortCurveLength ;
        return sensorPositions.ConvertAll( pos => sensorDirection switch
        {
          SensorArrayDirection.XMinus => new XYZ( pos.X + passPointOffset, sensorLineY, sensorLineZ ),
          SensorArrayDirection.YMinus => new XYZ( sensorLineX, pos.Y + passPointOffset, sensorLineZ ),
          SensorArrayDirection.XPlus => new XYZ( pos.X - passPointOffset, sensorLineY, sensorLineZ ),
          SensorArrayDirection.YPlus => new XYZ( sensorLineX, pos.Y - passPointOffset, sensorLineZ ),
          _ => throw new ArgumentOutOfRangeException( nameof( sensorDirection ), sensorDirection, null )
        } ) ;
      }

      static XYZ GetSensorLine( XYZ powerPosition, IReadOnlyList<XYZ> sensorPositions, XYZ lastSensorPosition, SensorArrayDirection sensorDirection, double bendingRadius, double fixedHeight )
      {
        var groups = CreateDirectionGroupRanges( sensorPositions, sensorDirection, bendingRadius * 2 ) ;
        var powerPosValue = EvaluatePosition( powerPosition, sensorDirection ) ;
        var lastSensorPosValue = EvaluatePosition( lastSensorPosition, sensorDirection ) ;
        var linePosValue = GetLinePosition( groups, powerPosValue, lastSensorPosValue, bendingRadius ) ;
        return sensorDirection switch
        {
          SensorArrayDirection.XMinus => new XYZ( powerPosition.X, linePosValue, fixedHeight ),
          SensorArrayDirection.YMinus => new XYZ( linePosValue, powerPosition.Y, fixedHeight ),
          SensorArrayDirection.XPlus => new XYZ( powerPosition.X, linePosValue, fixedHeight ),
          SensorArrayDirection.YPlus => new XYZ( linePosValue, powerPosition.Y, fixedHeight ),
          _ => throw new ArgumentOutOfRangeException( nameof( sensorDirection ), sensorDirection, null )
        } ;

        static double EvaluatePosition( XYZ pos, SensorArrayDirection sensorDirection )
        {
          return sensorDirection switch
          {
            SensorArrayDirection.XMinus => pos.Y,
            SensorArrayDirection.YMinus => pos.X,
            SensorArrayDirection.XPlus => pos.Y,
            SensorArrayDirection.YPlus => pos.X,
            _ => throw new ArgumentOutOfRangeException( nameof( sensorDirection ), sensorDirection, null )
          } ;
        }

        static List<(double Min, double Max)> CreateDirectionGroupRanges( IEnumerable<XYZ> sensorPositions, SensorArrayDirection sensorDirection, double bendingRadius )
        {
          var list = new List<(double Min, double Max)>() ;
          foreach ( var pos in sensorPositions ) {
            var p = EvaluatePosition( pos, sensorDirection ) - bendingRadius ;
            var index = list.BinarySearch( ( p, p ), SensorRangeComparer.Instance ) ;
            if ( 0 <= index ) continue ;

            index = ~index ;
            var canMergeIntoPrev = ( 0 < index ) && ( p <= list[ index - 1 ].Max ) ;
            var canMergeIntoNext = ( index < list.Count ) && ( list[ index ].Min <= p + bendingRadius * 2 ) ;
            if ( canMergeIntoPrev && canMergeIntoNext ) {
              list[ index - 1 ] = ( list[ index - 1 ].Min, list[ index ].Max ) ;
              list.RemoveAt( index ) ;
            }
            else if ( canMergeIntoPrev ) {
              var (min, max) = list[ index - 1 ] ;
              list[ index - 1 ] = ( min, Math.Max( max, p + bendingRadius * 2 ) ) ;
            }
            else if ( canMergeIntoNext ) {
              var (min, max) = list[ index ] ;
              list[ index ] = ( Math.Min( min, p ), max ) ;
            }
            else {
              list.Insert( index, ( p, p + bendingRadius * 2 ) ) ;
            }
          }

          return list ;
        }

        static double GetLinePosition( List<(double Min, double Max)> groups, double powerPosValue, double lastSensorPosValue, double bendingRadius )
        {
          var gapPos = groups.BinarySearch( ( lastSensorPosValue, lastSensorPosValue ), SensorRangeComparer.Instance ) ;
          var insertPos = ~gapPos ;
          if ( 0 < insertPos && lastSensorPosValue <= groups[ insertPos - 1 ].Max ) {
            // in sensor positions: find best gap
            var prevBendPos = lastSensorPosValue - bendingRadius ;
            var nextBendPos = lastSensorPosValue + bendingRadius ;
            var beforeGroupIndex = GetBeforeGroupIndex( groups, insertPos - 1, prevBendPos ) ;
            var afterGroupIndex = GetAfterGroupIndex( groups, insertPos, nextBendPos ) ;
            var beforeMid = Math.Min( prevBendPos, ( 0 < beforeGroupIndex ? ( groups[ beforeGroupIndex - 1 ].Max + groups[ beforeGroupIndex ].Min ) * 0.5 : groups[ beforeGroupIndex ].Min ) ) ;
            var afterMid = Math.Max( nextBendPos, ( afterGroupIndex < groups.Count ? ( groups[ afterGroupIndex - 1 ].Max + groups[ afterGroupIndex ].Min ) * 0.5 : groups[ afterGroupIndex - 1 ].Max ) ) ;

            // Use nearer position (except too near to power)
            var prevToPower = Math.Abs( beforeMid - powerPosValue ) ;
            if ( prevToPower < bendingRadius ) return afterMid ;

            var nextToPower = Math.Abs( afterMid - powerPosValue ) ;
            if ( nextToPower < bendingRadius ) return beforeMid ;

            return ( prevToPower < nextToPower ) ? beforeMid : afterMid ;
          }
          else {
            // in gap: can use lastSensorPosValue
            return lastSensorPosValue ;
          }

          static int GetBeforeGroupIndex( IReadOnlyList<(double Min, double Max)> groups, int beforeGroupIndex, double prevBendPos )
          {
            // if the next gap is nearer than powerPosValue - bendingRadius, find the previous gap
            while ( 0 < beforeGroupIndex && prevBendPos < groups[ beforeGroupIndex - 1 ].Max ) {
              --beforeGroupIndex ;
            }

            return beforeGroupIndex ;
          }

          static int GetAfterGroupIndex( IReadOnlyList<(double Min, double Max)> groups, int afterGroupIndex, double nextBendPos )
          {
            // if the next gap is nearer than powerPosValue + bendingRadius, find the next gap
            var n = groups.Count ;
            while ( afterGroupIndex < n && groups[ afterGroupIndex ].Min < nextBendPos ) {
              ++afterGroupIndex ;
            }

            return afterGroupIndex ;
          }
        }
      }
    }

    private class SensorRangeComparer : IComparer<(double Min, double Max)>
    {
      public static IComparer<(double Min, double Max)> Instance { get ; } = new SensorRangeComparer() ;

      private SensorRangeComparer()
      {
      }

      public int Compare( (double Min, double Max) x, (double Min, double Max) y )
      {
        return x.Min.CompareTo( y.Min ) ;
      }
    }

    private static double GetPreferredRouteHeight( XYZ powerPosition, IEnumerable<XYZ> sensorPositions, XYZ lastSensorPosition, double bendingRadius )
    {
      var sensorHeight = sensorPositions.Append( lastSensorPosition ).Max( pos => pos.Z ) ;
      var powerHeight = powerPosition.Z ;
      if ( powerHeight < sensorHeight + bendingRadius ) {
        return powerHeight + bendingRadius ;
      }
      else {
        return sensorHeight + bendingRadius ;
      }
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue )
    {
      ElectricalCommandUtil.SetConstructionItemForCable( document, executeResultValue ) ;
    }
  }
}