using System ;
using System.Collections.Generic ;
using System.Linq ;
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
    private const double MaxDistanceBetweenTextNotes = 1.5 ;
    
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
      var wireLengthNotationStorable = document.GetWireLengthNotationStorable() ;
      var wireLengthNotationData = wireLengthNotationStorable.WireLengthNotationData.Where( tp => tp.Level == level );
      var textNoteIds = document.GetAllElements<TextNote>().Where( t => wireLengthNotationData.Any( tp => tp.TextNoteId == t.UniqueId ) ).Select( t => t.Id ).ToList() ;
      
      document.Delete( textNoteIds ) ;

      wireLengthNotationStorable.WireLengthNotationData.RemoveAll( tp => tp.Level == level );
      wireLengthNotationStorable.Save() ;
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

        ShowPickUp( document, routeCache, allConduitsOfRoutes, isDisplayPickUpNumber, pickUpModelsByProductCode.PickUpModels,
          straightTextNoteOfPickUpFigureModels, obliqueTextNoteOfPickUpFigureModels, ref pickUpNumberOfPullBox ) ;
      }

      foreach ( var obliqueTextNoteOfPickUpFigureModel in obliqueTextNoteOfPickUpFigureModels ) {
        SetPositionForObliqueTextNoteOfPickUpFigure( routeCache, allConduitsOfRoutes, obliqueTextNoteOfPickUpFigureModel, scale ) ;
      }

      #region Set size of textnotes in entangled case

      allTextNoteOfPickUpFigureModels.AddRange( obliqueTextNoteOfPickUpFigureModels ) ;
      allTextNoteOfPickUpFigureModels.AddRange( straightTextNoteOfPickUpFigureModels ) ;
      var rooms = document.GetAllFamilyInstances( ElectricalRoutingFamilyType.Room ).ToList() ;
      var reSizeRooms = rooms.ToDictionary( x => x.Id.IntegerValue, _ => false ) ;
      foreach ( var textNoteOfPickUpFigureModel in allTextNoteOfPickUpFigureModels ) {
        if ( textNoteOfPickUpFigureModel.Position == null ) continue ;

        foreach ( var room in rooms ) {
          var isOutOfRoom = RoomRouteManager.CheckPickElementIsInOrOutRoom( room, textNoteOfPickUpFigureModel.Position ) ;
          if ( isOutOfRoom ) continue ;
          
          var reSizeRoomId = room.Id.IntegerValue ;

          if ( ! reSizeRooms[ reSizeRoomId ] && allTextNoteOfPickUpFigureModels.Any( x =>
              {
                if ( x.Position == null ) return false ;
                var xDistance = Math.Abs( x.Position.X - textNoteOfPickUpFigureModel.Position.X ) ;
                var yDistance = Math.Abs( x.Position.Y - textNoteOfPickUpFigureModel.Position.Y ) ;
                return xDistance < MaxDistanceBetweenTextNotes && yDistance < MaxDistanceBetweenTextNotes &&
                       xDistance >= MaxToleranceOfTextNotePosition && yDistance >= MaxToleranceOfTextNotePosition ;
              } ) && ! IsNearPowerConnector( document, textNoteOfPickUpFigureModel.Position ) )
            reSizeRooms[ reSizeRoomId ] = true ;

          textNoteOfPickUpFigureModel.RoomId = reSizeRoomId ;

          break ;
        }
      }

      #endregion
      
      foreach ( var textNoteOfPickUpFigureModel in allTextNoteOfPickUpFigureModels ) {
        if ( textNoteOfPickUpFigureModel.RoomId == null ) continue ;

        var textNote = CreateTextNote( document, textNoteOfPickUpFigureModel, reSizeRooms[ (int)textNoteOfPickUpFigureModel.RoomId ] ) ;
        if ( textNote != null )
          textNoteOfPickUpFigureModel.Id = textNote.UniqueId ;
      }

      SaveWireLengthNotationModel( wireLengthNotationStorable, allTextNoteOfPickUpFigureModels.Select( x => new WireLengthNotationModel( x.Id, level.Name ) ).ToList() ) ;
    }
    
    private static void ShowPickUp(Document document, RouteCache routes, IReadOnlyDictionary<string, List<Conduit>> allConduitsOfRoutes, bool isDisplayPickUpNumber, 
      IEnumerable<PickUpItemModel> pickUpModels, List<TextNoteOfPickUpFigureModel> straightTextNoteOfPickUpFigureModels, List<TextNoteOfPickUpFigureModel> obliqueTextNoteOfPickUpFigureModels, ref int pickUpNumberOfPullBox )
    {
      var pickUpModelsGroupsByRelatedRouteName = pickUpModels.GroupBy( p => p.RelatedRouteName ) ;
      var obliqueTextNoteOfPickUpFigureQuantities = new Dictionary<string, double>() ;
      foreach ( var pickUpModelsGroup in pickUpModelsGroupsByRelatedRouteName ) {
        var routeName = pickUpModelsGroup.Key ;
        var pickUpNumbers = WireLengthNotationManager.GetPickUpNumbersList( pickUpModelsGroup.AsEnumerable().ToList() ) ;
        var lastRoute = routes.LastOrDefault( r => r.Key == routeName ) ;
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

          var pullBoxUniqueId = PullBoxRouteManager.IsSegmentConnectedToPoPullBox( document, lastSegment ) ;
          var isToPullBox = ! string.IsNullOrEmpty( pullBoxUniqueId ) ;

          var nearestConduit = FindNearestConduit( document, conduitsOfLastRoute, toConnectorPosition, true ) ??
                               FindNearestConduit( document, conduitsOfLastRoute, toConnectorPosition ) ;
          if ( nearestConduit is { Location: LocationCurve location } ) {
            var line = ( location.Curve as Line )! ;
            var fromPoint = line.GetEndPoint( 0 ) ;
            var toPoint = line.GetEndPoint( 1 ) ;
            var direction = line.Direction ;
            var middlePoint = XyzUtil.GetMiddlePoint( fromPoint, toPoint, direction ) ;
            
            int counter;
            XYZ? position;
            var positionOfStraightTextNote = straightTextNoteOfPickUpFigureModels.Where( x => x.Position != null &&
              Math.Abs( x.RelatedPosition.X - middlePoint.X ) < MaxToleranceOfTextNotePosition && Math.Abs( x.RelatedPosition.Y - middlePoint.Y ) < MaxToleranceOfTextNotePosition )
              .OrderBy( x => x.Counter ).LastOrDefault();
            if ( positionOfStraightTextNote == default ) {
              counter = 1 ;
              position = middlePoint ;
            }
            else if ( ! isToPullBox ) {
              var isLeftOrTop = positionOfStraightTextNote.Counter % 2 != 0 ;
              
              if ( direction.Y is 1 or -1 )
                middlePoint = new XYZ( isLeftOrTop ? middlePoint.X + 1.7 + 1.5 * ( positionOfStraightTextNote.Counter - 1 ) / 2 : middlePoint.X - 1.5 * positionOfStraightTextNote.Counter / 2, middlePoint.Y, middlePoint.Z ) ;
              else if ( direction.X is 1 or -1 )
                middlePoint = new XYZ( middlePoint.X, isLeftOrTop ? middlePoint.Y - 1.7 - 1.5 * ( positionOfStraightTextNote.Counter - 1 ) / 2 : middlePoint.Y + 1.5 * positionOfStraightTextNote.Counter / 2, middlePoint.Z ) ;

              counter = positionOfStraightTextNote.Counter + 1 ;
              position = positionOfStraightTextNote.RelatedPosition ;
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
            var textNoteOfPickUpFigureModel = new TextNoteOfPickUpFigureModel( string.Empty, counter, position, middlePoint, strStraightTextNoteOfPickUpFigureQuantity, wireLengthNotationAlignment, null, null ) ;
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
          var textNoteOfPickUpFigureModel = new TextNoteOfPickUpFigureModel( string.Empty, 0, positionOfTextNote, positionOfTextNote, strObliqueTextNoteOfPickUpFigureQuantity, WireLengthNotationAlignment.Oblique, null, null ) ;
          obliqueTextNoteOfPickUpFigureModels.Add( textNoteOfPickUpFigureModel );
        }
      }
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

    private static TextNote? CreateTextNote(Document doc, TextNoteOfPickUpFigureModel textNoteOfPickUpFigureModel, bool isSmallSize = false )
    {
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( doc ) ;
      var baseLengthOfLine = scale / 100d ;
      var deviceSymbolTextNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( ShowCeedModelsCommandBase.DeviceSymbolTextNoteTypeName, tt.Name ) ) ;
      var fontSize = isSmallSize ? .005 : .01 ;
      if ( deviceSymbolTextNoteType != null ) {
        var deviceSymbolTextNoteFontSize = deviceSymbolTextNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).AsDouble() ;
        fontSize = isSmallSize ? deviceSymbolTextNoteFontSize / 2 : deviceSymbolTextNoteFontSize ;
      }
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

      TextNoteOptions textNoteOptions = new()
      {
        HorizontalAlignment = HorizontalTextAlignment.Center, 
        TypeId = textTypeId, 
        Rotation = rotation
      } ;

      if ( textNoteOfPickUpFigureModel.Position != null ) {
        if ( textNoteOfPickUpFigureModel.PickUpAlignment is WireLengthNotationAlignment.Vertical )
          textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X - 2 * ( baseLengthOfLine - 1 ), textNoteOfPickUpFigureModel.Position.Y, textNoteOfPickUpFigureModel.Position.Z ) ;
        
        else if ( textNoteOfPickUpFigureModel.PickUpAlignment is WireLengthNotationAlignment.Horizontal )
          textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X, textNoteOfPickUpFigureModel.Position.Y + 2 * ( baseLengthOfLine - 1 ), textNoteOfPickUpFigureModel.Position.Z ) ;

        if ( isSmallSize ) {
          if ( textNoteOfPickUpFigureModel.PickUpAlignment is WireLengthNotationAlignment.Vertical )
            textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X + 0.7 * baseLengthOfLine, textNoteOfPickUpFigureModel.Position.Y, textNoteOfPickUpFigureModel.Position.Z ) ;
          
          else if ( textNoteOfPickUpFigureModel.PickUpAlignment is WireLengthNotationAlignment.Oblique ) {
            var textNoteDirection = textNoteOfPickUpFigureModel.Direction ;
            
            if ( textNoteDirection == null )
              textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X, textNoteOfPickUpFigureModel.Position.Y, textNoteOfPickUpFigureModel.Position.Z ) ;
            
            else if ( textNoteDirection.Y is 1 )
              textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X, textNoteOfPickUpFigureModel.Position.Y - 0.8 * baseLengthOfLine, textNoteOfPickUpFigureModel.Position.Z ) ;
            
            else if ( textNoteDirection.Y is -1 )
              textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X, textNoteOfPickUpFigureModel.Position.Y + 0.5 * baseLengthOfLine, textNoteOfPickUpFigureModel.Position.Z ) ;
            
            else if ( textNoteDirection.X is 1 )
              textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X - 0.1 * baseLengthOfLine, textNoteOfPickUpFigureModel.Position.Y, textNoteOfPickUpFigureModel.Position.Z ) ;
            
            else if ( textNoteDirection.X is -1 )
              textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X + 1.3 * baseLengthOfLine, textNoteOfPickUpFigureModel.Position.Y, textNoteOfPickUpFigureModel.Position.Z ) ;
          }
          
          else
            textNoteOfPickUpFigureModel.Position = new XYZ( textNoteOfPickUpFigureModel.Position.X, textNoteOfPickUpFigureModel.Position.Y - 0.8 * baseLengthOfLine, textNoteOfPickUpFigureModel.Position.Z ) ;
        }
      }
      
      var textNoteOfPickUpFigure = TextNote.Create( doc, doc.ActiveView.Id, textNoteOfPickUpFigureModel.Position, textNoteOfPickUpFigureModel.Content, textNoteOptions ) ;
      
      var colorOfTextNote = new Color( 255, 225, 51 ) ; // Dark yellow
      ConfirmUnsetCommandBase.ChangeElementColor( new []{ textNoteOfPickUpFigure }, colorOfTextNote ) ;

      return textNoteOfPickUpFigure ;
    }

    private static void SaveWireLengthNotationModel(WireLengthNotationStorable wireLengthNotationStorable, IEnumerable<WireLengthNotationModel> wireLengthNotationModels)
    {
      try {
        wireLengthNotationStorable.WireLengthNotationData.AddRange(wireLengthNotationModels) ;
        wireLengthNotationStorable.Save() ;
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
          var pullBoxUniqueId = PullBoxRouteManager.IsSegmentConnectedToPoPullBox( document, lastSegment ) ;
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
    
    private static bool IsNearPowerConnector( Document document, XYZ point )
    {
      var allPowerConnectors = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .Where( c => c.GetConnectorFamilyType() == ConnectorFamilyType.Power ) ;
      return allPowerConnectors.Any( c =>
      {
        var locationPoint =  ( c.Location as LocationPoint )?.Point ;
        return locationPoint != null && ( Math.Abs( locationPoint.X - point.X ) < MaxDistanceBetweenTextNotes || Math.Abs( locationPoint.Y - point.Y ) < MaxDistanceBetweenTextNotes ) ;
      } ) ;
    } 
  }
}