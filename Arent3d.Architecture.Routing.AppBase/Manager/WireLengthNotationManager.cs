using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.Utils ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public static class WireLengthNotationManager
  {
    private const double MaxToleranceOfTextNotePosition = 0.001 ;
    private static readonly double MaxDistanceBetweenTextNotes = 950.0.MillimetersToRevitUnits() ;
    private const double MaxIntersectVolumeBetweenTextNotes = 0.4 ;

    private static List<string> GetPickUpNumbersList( IEnumerable<PickUpItemModel> pickUpModels )
    {
      var pickUpNumberList = new List<string>() ;
      
      foreach ( var pickUpModel in pickUpModels.Where( pickUpModel => ! pickUpNumberList.Contains( pickUpModel.PickUpNumber ) ) ) {
        pickUpNumberList.Add( pickUpModel.PickUpNumber ) ;
      }

      return pickUpNumberList ;
    }
    
    public static void RemoveWireLengthNotation( Document document, string level )
    {
      using var transaction = new Transaction( document, "Remove wire length notation" ) ;
      transaction.Start() ;
      var wireLengthNotationStorable = document.GetWireLengthNotationStorable() ;
      var wireLengthNotationData = wireLengthNotationStorable.WireLengthNotationData.Where( tp => tp.Level == level );
      var textNoteIds = document.GetAllElements<TextNote>().Where( t => wireLengthNotationData.Any( tp => tp.TextNoteId == t.UniqueId ) ).Select( t => t.Id ).ToList() ;
      
      document.Delete( textNoteIds ) ;

      wireLengthNotationStorable.WireLengthNotationData.RemoveAll( tp => tp.Level == level );
      wireLengthNotationStorable.Save() ;
      transaction.Commit() ;
    }
    
    public static void ShowWireLengthNotation( WireLengthNotationStorable wireLengthNotationStorable, Document document, Level level, List<PickUpItemModel> pickUpModels )
    {
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var pickUpNumberOfPullBox = 0 ;
      var pickUpModelsWithPickUpNumber = pickUpModels.Where( x => ! string.IsNullOrEmpty( x.PickUpNumber ) ).ToList() ;
      if ( pickUpModelsWithPickUpNumber.Any() )
        pickUpNumberOfPullBox = pickUpModelsWithPickUpNumber.Max( x => Convert.ToInt32( x.PickUpNumber ) ) ;
      var isDisplayPickUpNumber = wireLengthNotationStorable.PickUpNumberSettingData[level.Id.IntegerValue]?.IsPickUpNumberSetting ?? false ;
      var routes = pickUpModels.Select( x => x.RouteName ).Where( r => r != "" ).Distinct() ;
      var straightTextNoteOfPickUpFigureModels = new List<TextNoteOfPickUpFigureModel>() ;
      var obliqueTextNoteOfPickUpFigureModels = new List<TextNoteOfPickUpFigureModel>() ;
      var allTextNoteOfPickUpFigureModels = new List<TextNoteOfPickUpFigureModel>() ;
      var allConduits = new FilteredElementCollector( document ).OfCategory( BuiltInCategory.OST_Conduit ).OfType<Conduit>().ToList() ;
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).Where( e => e.Name != ElectricalRoutingFamilyType.PullBox.GetFamilyName() ).ToList() ;
      var routeCache = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var allConduitsOfRoutes = new Dictionary<string, List<Conduit>>() ;
      foreach ( var route in routeCache ) {
        var conduitsOfRoute = allConduits.Where( conduit =>
          conduit.GetRouteName() is { } rName && rName == route.Key ).ToList() ;
        if( conduitsOfRoute.Count > 0 )
          allConduitsOfRoutes.Add( route.Key, conduitsOfRoute );
      }

      foreach ( var route in routes ) {
        var pickUpModelsByProductCode = pickUpModels
          .Where( p => p.RouteName == route && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() )
          .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } )
          .FirstOrDefault() ;
        if ( pickUpModelsByProductCode == null ) continue ;

        ShowPickUp( document, routeCache, allConduitsOfRoutes, allConnectors, isDisplayPickUpNumber, pickUpModelsByProductCode.PickUpModels,
          straightTextNoteOfPickUpFigureModels, obliqueTextNoteOfPickUpFigureModels, ref pickUpNumberOfPullBox ) ;
      }

      foreach ( var obliqueTextNoteOfPickUpFigureModel in obliqueTextNoteOfPickUpFigureModels ) {
        SetPositionForObliqueTextNoteOfPickUpFigure( routeCache, allConduitsOfRoutes, obliqueTextNoteOfPickUpFigureModel, scale ) ;
      }

      allTextNoteOfPickUpFigureModels.AddRange( obliqueTextNoteOfPickUpFigureModels ) ;
      allTextNoteOfPickUpFigureModels.AddRange( straightTextNoteOfPickUpFigureModels ) ;
      var reSizeBoards = allTextNoteOfPickUpFigureModels.Where( t => !string.IsNullOrEmpty( t.BoardId ) )
        .Select( x => x.BoardId + "_" + true ).Distinct().ToDictionary( x => x, _ => false ) ;
      reSizeBoards.AddRange( allTextNoteOfPickUpFigureModels.Where( t => !string.IsNullOrEmpty( t.BoardId ) )
        .Select( x => x.BoardId + "_" + false ).Distinct().ToDictionary( x => x, _ => false ) ) ;
      
      using var createTextNoteTransaction = new Transaction( document, "Create Text notes" ) ;
      createTextNoteTransaction.Start() ;
      foreach ( var textNoteOfPickUpFigureModel in allTextNoteOfPickUpFigureModels ) {
        var textNote = CreateTextNote( document, textNoteOfPickUpFigureModel ) ;
        if ( textNote != null )
          textNoteOfPickUpFigureModel.TextNote = textNote ;
      }
      createTextNoteTransaction.Commit() ;

      #region Set size of textnotes in entangled case
      
      foreach ( var textNoteOfPickUpFigureModel in allTextNoteOfPickUpFigureModels ) {
        if ( textNoteOfPickUpFigureModel.Position == null || string.IsNullOrEmpty( textNoteOfPickUpFigureModel.BoardId ) || textNoteOfPickUpFigureModel.TextNote == null ) continue ;

        if ( ! reSizeBoards[ textNoteOfPickUpFigureModel.BoardId + "_" + textNoteOfPickUpFigureModel.IsToConnector ] && allTextNoteOfPickUpFigureModels.Any( x =>
            {
              if ( x.Position == null || string.IsNullOrEmpty( x.BoardId ) || x.TextNote == null || x.TextNote.UniqueId == textNoteOfPickUpFigureModel.TextNote.UniqueId ) return false ;
              var xDistance = Math.Abs( x.Position.X - textNoteOfPickUpFigureModel.Position.X ) ;
              var yDistance = Math.Abs( x.Position.Y - textNoteOfPickUpFigureModel.Position.Y ) ;
              if ( xDistance < MaxToleranceOfTextNotePosition && yDistance < MaxToleranceOfTextNotePosition ) return false ;
              
              var intersectSolid = GeometryHelper.GetSolidExecutionOfTextNotes( document, x.TextNote, textNoteOfPickUpFigureModel.TextNote, BooleanOperationsType.Intersect ) ;
              return ! ( Math.Abs( intersectSolid.Volume ) <= MaxIntersectVolumeBetweenTextNotes ) ;
            } ) && ! IsNearPowerConnector( document, textNoteOfPickUpFigureModel.Position ) )
          reSizeBoards[ textNoteOfPickUpFigureModel.BoardId + "_" + textNoteOfPickUpFigureModel.IsToConnector ] = true ;
      }
      
      var baseLengthOfLine = scale / 100d ;
      using var changeTextNoteSizeTransaction = new Transaction( document, "Change text note size" ) ;
      changeTextNoteSizeTransaction.Start() ;
      foreach ( var reSizeBoard in reSizeBoards ) {
        if ( ! reSizeBoard.Value ) continue ;
        
        allTextNoteOfPickUpFigureModels.Where( t => t.BoardId + "_" + t.IsToConnector == reSizeBoard.Key ).ForEach( t =>
        {
          if ( t.TextNote != null && t.Position != null ) {
            var textSize = ( t.TextNote!.TextNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).AsDouble() / 2 ).RevitUnitsToMillimeters() ;
            if ( t.PickUpAlignment is WireLengthNotationAlignment.Vertical )
              t.Position = new XYZ( t.Position.X + 0.7 * baseLengthOfLine, t.Position.Y, t.Position.Z ) ;
          
            else if ( t.PickUpAlignment is WireLengthNotationAlignment.Oblique ) {
              var textNoteDirection = t.Direction ;
            
              if ( textNoteDirection == null )
                t.Position = new XYZ( t.Position.X, t.Position.Y, t.Position.Z ) ;
            
              else if ( textNoteDirection.Y is 1 )
                t.Position = new XYZ( t.Position.X, t.Position.Y - 0.8 * baseLengthOfLine, t.Position.Z ) ;
            
              else if ( textNoteDirection.Y is -1 )
                t.Position = new XYZ( t.Position.X, t.Position.Y + 0.5 * baseLengthOfLine, t.Position.Z ) ;
            
              else if ( textNoteDirection.X is 1 )
                t.Position = new XYZ( t.Position.X - 0.1 * baseLengthOfLine, t.Position.Y, t.Position.Z ) ;
            
              else if ( textNoteDirection.X is -1 )
                t.Position = new XYZ( t.Position.X + 1.3 * baseLengthOfLine, t.Position.Y, t.Position.Z ) ;
            }
          
            else
              t.Position = new XYZ( t.Position.X, t.Position.Y - 0.8 * baseLengthOfLine, t.Position.Z ) ;
      
            t.TextNote!.Coord = t.Position ;
            t.TextNote!.TextNoteType = TextNoteHelper.FindOrCreateTextNoteType( document, textSize, false ) ;
          }
        } );
      }
      
      changeTextNoteSizeTransaction.Commit() ;

      #endregion
      
      SaveWireLengthNotationModel( document, wireLengthNotationStorable, allTextNoteOfPickUpFigureModels.Select( x => new WireLengthNotationModel( x.TextNote!.UniqueId, level.Name ) ).ToList() ) ;
    }
    
    private static void ShowPickUp(Document document, RouteCache routes, IReadOnlyDictionary<string, List<Conduit>> allConduitsOfRoutes, List<Element> allConnectors, bool isDisplayPickUpNumber, 
      IEnumerable<PickUpItemModel> pickUpModels, List<TextNoteOfPickUpFigureModel> straightTextNoteOfPickUpFigureModels, List<TextNoteOfPickUpFigureModel> obliqueTextNoteOfPickUpFigureModels, ref int pickUpNumberOfPullBox )
    {
      var pickUpModelsGroupsByRelatedRouteName = pickUpModels.GroupBy( p => p.RelatedRouteName ) ;
      var obliqueTextNoteOfPickUpFigureQuantities = new Dictionary<string, double>() ;
      foreach ( var pickUpModelsGroup in pickUpModelsGroupsByRelatedRouteName ) {
        var routeName = pickUpModelsGroup.Key ;
        var pickUpNumbers = GetPickUpNumbersList( pickUpModelsGroup.AsEnumerable().ToList() ) ;
        var lastRoute = routes.LastOrDefault( r => r.Key == routeName ) ;
        var fromConnector = ConduitUtil.GetConnectorOfRoute( document, routeName, true ) ;
        
        var lastSegment = lastRoute.Value.RouteSegments.Last() ;
        
        double straightTextNoteOfPickUpFigureQuantity = 0 ;
        foreach ( var pickUpNumber in pickUpNumbers ) {
          var pickUpModelsInGroup = pickUpModelsGroup.AsEnumerable().Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
          
          foreach ( var item in pickUpModelsInGroup.Where( item => ! string.IsNullOrEmpty( item.Quantity ) ) ) {
            double.TryParse( item.Quantity, out var quantity ) ;
            if ( string.IsNullOrEmpty( item.Direction ) )
              straightTextNoteOfPickUpFigureQuantity += Math.Round( quantity, 1 ) ;
            else {
              if ( ! obliqueTextNoteOfPickUpFigureQuantities.Keys.Contains( item.Direction ) )
                obliqueTextNoteOfPickUpFigureQuantities.Add( item.Direction, 0 ) ;

              obliqueTextNoteOfPickUpFigureQuantities[ item.Direction ] += Math.Round( quantity, 1 ) ;
            }
          }

          // Quantity of StraightTextNoteOfPickUpFigure
          if ( ! allConduitsOfRoutes.ContainsKey( lastRoute.Key ) ) continue ;
          
          var conduitsOfLastRoute = allConduitsOfRoutes[lastRoute.Key] ;
          var toConnectorPosition = lastSegment.ToEndPoint.RoutingStartPosition ;

          var pullBoxUniqueId = PullBoxRouteManager.IsSegmentConnectedToPullBox( document, lastSegment ) ;
          var isToPullBox = ! string.IsNullOrEmpty( pullBoxUniqueId ) ;

          var nearestConduit = FindNearestConduit( document, conduitsOfLastRoute, toConnectorPosition, true ) ??
                               FindNearestConduit( document, conduitsOfLastRoute, toConnectorPosition ) ;
          if ( nearestConduit is { Location: LocationCurve location } ) {
            var line = ( location.Curve as Line )! ;
            var fromPoint = line.GetEndPoint( 0 ) ;
            var toPoint = line.GetEndPoint( 1 ) ;
            var direction = line.Direction ;
            var middlePointOfConduit = XyzUtil.GetMiddlePoint( fromPoint, toPoint ) ;
            middlePointOfConduit = new XYZ( middlePointOfConduit.X, middlePointOfConduit.Y, 0 ) ;
            var middlePoint = GetPositionOfStraightTextNote( middlePointOfConduit, direction ) ;
            
            int counter;
            XYZ? position;
            var positionOfStraightTextNote = straightTextNoteOfPickUpFigureModels
              .Where( x => Math.Abs( x.RelatedPosition.X - middlePointOfConduit.X ) < MaxToleranceOfTextNotePosition && Math.Abs( x.RelatedPosition.Y - middlePointOfConduit.Y ) < MaxToleranceOfTextNotePosition )
              .OrderBy( x => x.Counter ).LastOrDefault();
            if ( positionOfStraightTextNote == default ) {
              counter = 1 ;
              position = middlePoint ;
            }
            else if ( ! isToPullBox ) {
              var isLeftOrTop = positionOfStraightTextNote.Counter % 2 != 0 ;
              
              position = GetPositionOfStraightTextNote( positionOfStraightTextNote.RelatedPosition, direction ) ;
              if ( direction.Y is 1 or -1 )
                position = new XYZ( isLeftOrTop ? middlePoint.X + 1.7 + 1.5 * ( positionOfStraightTextNote.Counter - 1 ) / 2 : middlePoint.X - 1.5 * positionOfStraightTextNote.Counter / 2, middlePoint.Y, middlePoint.Z ) ;
              else if ( direction.X is 1 or -1 )
                position = new XYZ( middlePoint.X, isLeftOrTop ? middlePoint.Y - 1.7 - 1.5 * ( positionOfStraightTextNote.Counter - 1 ) / 2 : middlePoint.Y + 1.5 * positionOfStraightTextNote.Counter / 2, middlePoint.Z ) ;

              counter = positionOfStraightTextNote.Counter + 1 ;
            }
            else continue ;

            var textOfPickUpNumber = string.Empty ;
            if ( isDisplayPickUpNumber ) {
              textOfPickUpNumber = pickUpNumber ;
              if ( isToPullBox )
                textOfPickUpNumber = pickUpNumberOfPullBox != 0 ? ( ++pickUpNumberOfPullBox ).ToString() : string.Empty ;
              textOfPickUpNumber = string.IsNullOrEmpty( textOfPickUpNumber ) ? string.Empty : "[" + textOfPickUpNumber + "]" ;
            }
            
            var strStraightTextNoteOfPickUpFigureQuantity = textOfPickUpNumber + straightTextNoteOfPickUpFigureQuantity;
            var wireLengthNotationAlignment = WireLengthNotationAlignment.Horizontal ;
            if ( direction is { Y: 1 or -1 } )
              wireLengthNotationAlignment = WireLengthNotationAlignment.Vertical ;
            var isToConnector = false ;
            var toEndPoint = nearestConduit.GetNearestEndPoints( false ).ToList() ;

            if ( toEndPoint.Any() ) {
              var toEndPointKey = toEndPoint.First().Key ;
              var toElementId = toEndPointKey.GetElementUniqueId() ;
              if ( ! string.IsNullOrEmpty( toElementId ) ) {
                var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementId ) ;
                if ( toConnector != null && ! toConnector.IsTerminatePoint() && ! toConnector.IsPassPoint() )
                  isToConnector = true ;
              }
            }
              
            var textNoteOfPickUpFigureModel = new TextNoteOfPickUpFigureModel( null, counter, positionOfStraightTextNote?.RelatedPosition ?? middlePointOfConduit, position, strStraightTextNoteOfPickUpFigureQuantity, wireLengthNotationAlignment, null, fromConnector?.UniqueId, isToConnector ) ;
            straightTextNoteOfPickUpFigureModels.Add( textNoteOfPickUpFigureModel ) ;
          }
        }
        
        // Quantity of ObliqueTextNoteOfPickUpFigure
        foreach ( var obliqueTextNoteOfPickUpFigureQuantity in obliqueTextNoteOfPickUpFigureQuantities ) {
          var points = obliqueTextNoteOfPickUpFigureQuantity.Key.Split( ',' ) ;
          var xPoint = double.Parse( points.First() ) ;
          var yPoint = double.Parse( points.Skip( 1 ).First() ) ;

          if ( obliqueTextNoteOfPickUpFigureModels.Any( x => Math.Abs( x.RelatedPosition.X - xPoint ) < MaxToleranceOfTextNotePosition && Math.Abs( x.RelatedPosition.Y - yPoint ) < MaxToleranceOfTextNotePosition ) ) 
            continue ;

          var strObliqueTextNoteOfPickUpFigureQuantity = "↓ " + obliqueTextNoteOfPickUpFigureQuantity.Value ;
          var positionOfTextNote = new XYZ( xPoint, yPoint, 0 ) ;
          var textNoteOfPickUpFigureModel = new TextNoteOfPickUpFigureModel( null, 0, positionOfTextNote, positionOfTextNote, strObliqueTextNoteOfPickUpFigureQuantity, WireLengthNotationAlignment.Oblique, null, fromConnector?.UniqueId, true ) ;
          obliqueTextNoteOfPickUpFigureModels.Add( textNoteOfPickUpFigureModel );
        }
      }
    }
    
    private static XYZ GetPositionOfStraightTextNote( XYZ middlePoint, XYZ direction )
    {
      if (direction.Y is 1 or -1) return new XYZ( middlePoint.X - 1.5 , middlePoint.Y, middlePoint.Z ) ;
      
      if (direction.X is 1 or -1) return new XYZ( middlePoint.X, middlePoint.Y + 1.5, middlePoint.Z ) ;

      return middlePoint ;
    }
    
    private static void SetPositionForObliqueTextNoteOfPickUpFigure( RouteCache routeCache, Dictionary<string, List<Conduit>> conduitsOfRoutesForObliqueTextNote,
      TextNoteOfPickUpFigureModel obliqueTextNoteOfPickUpFigureModel, double scale )
    {
      var baseLengthOfLine = scale / 100d ;
      var conduitDirections = new List<XYZ>() ;
      var relatedPositionOfTextNote = obliqueTextNoteOfPickUpFigureModel.RelatedPosition ;
      var positionOfTextNote = obliqueTextNoteOfPickUpFigureModel.Position ;

      foreach ( var route in routeCache ) {
        var routeSegments = route.Value.RouteSegments ;
        if ( ! conduitsOfRoutesForObliqueTextNote.ContainsKey( route.Key ) ) continue ;
        
        var conduitsForObliqueTextNote = conduitsOfRoutesForObliqueTextNote[ route.Key ] ;
        GetConduitDirectionsForObliqueTextNoteOfPickUpFigure( conduitDirections, routeSegments, relatedPositionOfTextNote, conduitsForObliqueTextNote, true ) ;
        GetConduitDirectionsForObliqueTextNoteOfPickUpFigure( conduitDirections, routeSegments, relatedPositionOfTextNote, conduitsForObliqueTextNote ) ;
      }

      var defaultDirections = new List<XYZ> { new(-1, 0, 0), new(1, 0, 0), new(0, -1, 0), new(0, 1, 0) } ;
      var textNoteDirection = defaultDirections.FirstOrDefault( d => ! conduitDirections.Any( cd => cd.IsAlmostEqualTo( d ) ) ) ;

      if ( textNoteDirection == null )
        positionOfTextNote = new XYZ( relatedPositionOfTextNote.X + 1 * baseLengthOfLine, relatedPositionOfTextNote.Y + 2 * baseLengthOfLine, relatedPositionOfTextNote.Z ) ;
      else if ( textNoteDirection.Y is 1 )
        positionOfTextNote = new XYZ( relatedPositionOfTextNote.X - 0.2 * baseLengthOfLine, relatedPositionOfTextNote.Y + 1.8 * baseLengthOfLine, relatedPositionOfTextNote.Z ) ;
      else if ( textNoteDirection.Y is -1 )
        positionOfTextNote = new XYZ( relatedPositionOfTextNote.X, relatedPositionOfTextNote.Y - 1.2 * baseLengthOfLine, relatedPositionOfTextNote.Z ) ;
      else if ( textNoteDirection.X is 1 )
        positionOfTextNote = new XYZ( relatedPositionOfTextNote.X + 0.5 * baseLengthOfLine, relatedPositionOfTextNote.Y, relatedPositionOfTextNote.Z ) ;
      else if ( textNoteDirection.X is -1 )
        positionOfTextNote = new XYZ( relatedPositionOfTextNote.X - 2.5 * baseLengthOfLine, relatedPositionOfTextNote.Y, relatedPositionOfTextNote.Z ) ;
      
      obliqueTextNoteOfPickUpFigureModel.Position = positionOfTextNote ;
      obliqueTextNoteOfPickUpFigureModel.Direction = textNoteDirection ;
    }
    
    private static void GetConduitDirectionsForObliqueTextNoteOfPickUpFigure(List<XYZ> conduitDirections, IEnumerable<RouteSegment> routeSegments, XYZ positionOfObliqueTextNote, List<Conduit> conduitsForObliqueTextNote, bool isFrom = false )
    {
      var routingStartPosition = isFrom ? routeSegments.First().FromEndPoint.RoutingStartPosition : routeSegments.Last().ToEndPoint.RoutingStartPosition ;

      if ( ! ( Math.Abs( routingStartPosition.X - positionOfObliqueTextNote.X ) < MaxToleranceOfTextNotePosition ) ||
           ! ( Math.Abs( routingStartPosition.Y - positionOfObliqueTextNote.Y ) < MaxToleranceOfTextNotePosition ) )
        return ;

      foreach ( var conduitOfRoute in conduitsForObliqueTextNote ) {
        if ( conduitOfRoute.Location is not LocationCurve conduitLocation ) continue ;

        var line = ( conduitLocation.Curve as Line )! ;
        var direction = isFrom ? line.Direction : line.Direction.Negate() ;
        if ( direction.Z is 1 or -1 ) continue ;

        var toPoint = line.GetEndPoint( 1 ) ;
        var toPoint2D = new XYZ( toPoint.X, toPoint.Y, routingStartPosition.Z ) ;
        var distance = routingStartPosition.DistanceTo( toPoint2D ) ;
        
        if( !conduitDirections.Any( cd => cd.IsAlmostEqualTo( direction )) && distance < 1.5 )
          conduitDirections.Add( direction ) ;
        if ( conduitDirections.Count == 4 ) break ;
      }
    }

    private static Conduit? FindNearestConduit(Document document,List<Conduit> conduitsOfRoute, XYZ toPosition, bool isCheckLengthConduit = false)
    {
      Conduit? result = null ;
      
      var minDistance = double.MaxValue ;
      foreach ( var conduitOfRoute in conduitsOfRoute ) {
        if ( conduitOfRoute.Location is not LocationCurve conduitLocation ) continue ;
        
        var line = ( conduitLocation.Curve as Line )! ;
        var direction = line.Direction ;
        if ( direction.Z is 1 or -1 ) continue ;
        
        var toPoint = line.GetEndPoint( 1 ) ;
        var distance = toPosition.DistanceTo( toPoint ) ;
        var lengthConduit = conduitOfRoute.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ).AsDouble() ;
        if ( distance >= minDistance || ( isCheckLengthConduit && lengthConduit < 1.0 ) ) continue ;
        
        minDistance = distance ;
        result = conduitOfRoute ;
      }

      return result ;
    }

    private static TextNote? CreateTextNote( Document doc, TextNoteOfPickUpFigureModel textNoteOfPickUpFigureModel )
    {
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( doc ) ;
      var baseLengthOfLine = scale / 100d ;
      var deviceSymbolTextNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( CeedViewModel.DeviceSymbolTextNoteTypeName, tt.Name ) ) ;
      var fontSize = .01 ;
      if ( deviceSymbolTextNoteType != null )
        fontSize = deviceSymbolTextNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).AsDouble() ;
      fontSize = fontSize.RevitUnitsToMillimeters() ;

      var textNoteType = TextNoteHelper.FindOrCreateTextNoteType( doc, fontSize, false ) ;
      if ( textNoteType == null ) return null ;

      var textTypeId = textNoteType.Id ;

      var rotation = textNoteOfPickUpFigureModel.PickUpAlignment switch
      {
        WireLengthNotationAlignment.Oblique => Math.PI / 4,
        WireLengthNotationAlignment.Vertical => Math.PI / 2,
        _ => 0
      } ;

      TextNoteOptions textNoteOptions = new() { HorizontalAlignment = HorizontalTextAlignment.Center, TypeId = textTypeId, Rotation = rotation } ;

      if ( textNoteOfPickUpFigureModel.Position != null ) {
        if ( textNoteOfPickUpFigureModel.PickUpAlignment is WireLengthNotationAlignment.Vertical )
          textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X - 2 * ( baseLengthOfLine - 1 ), textNoteOfPickUpFigureModel.Position.Y, textNoteOfPickUpFigureModel.Position.Z ) ;

        else if ( textNoteOfPickUpFigureModel.PickUpAlignment is WireLengthNotationAlignment.Horizontal )
          textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X, textNoteOfPickUpFigureModel.Position.Y + 2 * ( baseLengthOfLine - 1 ), textNoteOfPickUpFigureModel.Position.Z ) ;
      }

      var textNoteOfPickUpFigure = TextNote.Create( doc, doc.ActiveView.Id, textNoteOfPickUpFigureModel.Position, textNoteOfPickUpFigureModel.Content, textNoteOptions ) ;

      var colorOfTextNote = new Color( 255, 225, 51 ) ; // Dark yellow
      ConfirmUnsetCommandBase.ChangeElementColor( new[] { textNoteOfPickUpFigure }, colorOfTextNote ) ;

      return textNoteOfPickUpFigure ;
    }

    private static void SaveWireLengthNotationModel(Document document, WireLengthNotationStorable wireLengthNotationStorable, IEnumerable<WireLengthNotationModel> wireLengthNotationModels)
    {
      try {
        using var transaction = new Transaction( document, "Save WireLengthNotation Data" ) ;
        transaction.Start() ;
        wireLengthNotationStorable.WireLengthNotationData.AddRange(wireLengthNotationModels) ;
        wireLengthNotationStorable.Save() ;
        transaction.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
    
    public static Dictionary<string, int> GetPickUpNumberForConduitsToPullBox( Document document, List<PickUpItemModel> pickUpModelsByLevel )
    {
      var result = new Dictionary<string, int>() ;
      var pickUpModelsWithPickUpNumber = pickUpModelsByLevel.Where( x => ! string.IsNullOrEmpty( x.PickUpNumber ) ).ToList() ;
      if ( ! pickUpModelsWithPickUpNumber.Any() ) return result ;
      
      var pullBoxIdWithPickUpNumbers = new Dictionary<string, int>() ;
      var routeCache = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var pickUpNumberOfPullBox = pickUpModelsWithPickUpNumber.Max( x => Convert.ToInt32( x.PickUpNumber ) ) ;
      var routes = pickUpModelsByLevel.Select( x => x.RouteName ).Where( r => r != "" ).Distinct() ;
      foreach ( var route in routes ) {
        var conduitPickUpModel = pickUpModelsByLevel
          .Where( p => p.RouteName == route && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() )
          .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } )
          .FirstOrDefault() ;
        if ( conduitPickUpModel == null ) continue ;
    
        var pickUpModelsGroupsByRouteNameRef = conduitPickUpModel.PickUpModels.GroupBy( p => p.RelatedRouteName ) ;
        foreach ( var pickUpModelsGroup in pickUpModelsGroupsByRouteNameRef ) {
          var routeName = pickUpModelsGroup.Key ;
          var lastRoute = routeCache.LastOrDefault( r => r.Key == routeName ) ;
          var lastSegment = lastRoute.Value.RouteSegments.Last() ;
          var pullBoxUniqueId = PullBoxRouteManager.IsSegmentConnectedToPullBox( document, lastSegment ) ;
          if ( string.IsNullOrEmpty( pullBoxUniqueId ) ) continue ;
            
          if ( pullBoxIdWithPickUpNumbers.ContainsKey( pullBoxUniqueId ) )
            result.Add( routeName, pullBoxIdWithPickUpNumbers[pullBoxUniqueId] );
          else {
            pickUpNumberOfPullBox++ ;
            pullBoxIdWithPickUpNumbers.Add( pullBoxUniqueId, pickUpNumberOfPullBox );
            result.Add( routeName, pickUpNumberOfPullBox );
          }
        }
      }
      
      return result; 
    }
    
    private static bool IsNearPowerConnector( Document document, XYZ elementPoint )
    {
      var allPowerConnectors = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .Where( c => c.GetConnectorFamilyType() == ConnectorFamilyType.Power ) ;
      return allPowerConnectors.Any( c =>
      {
        var powerLocationPoint = ( c.Location as LocationPoint )?.Point ;
        return powerLocationPoint != null && XyzUtil.GetDistanceIn2D( powerLocationPoint, elementPoint ) < MaxDistanceBetweenTextNotes ;
      } ) ;
    }
  }
}