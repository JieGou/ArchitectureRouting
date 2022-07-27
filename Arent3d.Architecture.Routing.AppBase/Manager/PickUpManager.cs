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
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public static class PickUpManager
  {
    private const double MaxToleranceOfTextNotePosition = 0.001 ;
    private const double MaxDistanceBetweenTextNotes = 1.5 ;
    
    private static List<string> GetPickUpNumbersList( IEnumerable<PickUpModel> pickUpModels )
    {
      var pickUpNumberList = new List<string>() ;
      
      foreach ( var pickUpModel in pickUpModels.Where( pickUpModel => ! pickUpNumberList.Contains( pickUpModel.PickUpNumber ) ) ) {
        pickUpNumberList.Add( pickUpModel.PickUpNumber ) ;
      }

      return pickUpNumberList ;
    }
    
    public static void RemoveTextNotePickUp( Document document, string level )
    {
      var textNotePickUpStorable = document.GetTextNotePickUpStorable() ;
      var textNotePickUpData = textNotePickUpStorable.TextNotePickUpData.Where( tp => tp.Level == level );
      var textNoteIds = document.GetAllElements<TextNote>().Where( t => textNotePickUpData.Any( tp => tp.TextNoteId == t.UniqueId ) ).Select( t => t.Id ).ToList() ;
      
      document.Delete( textNoteIds ) ;

      textNotePickUpStorable.TextNotePickUpData.RemoveAll( tp => tp.Level == level );
      textNotePickUpStorable.Save() ;
    }
    
    public static void ShowTextNotePickUp( TextNotePickUpModelStorable textNotePickUpStorable, Document document, Level level, List<PickUpModel> pickUpModels )
    {
      var x = GetPickUpNumberForConduitsToPullBox( document, pickUpModels ) ;
      var pickUpNumberOfPullBox = pickUpModels.Where( x => !string.IsNullOrEmpty( x.PickUpNumber ) ).Max( x => Convert.ToInt32( x.PickUpNumber ) ) ;
      var isDisplayPickUpNumber = textNotePickUpStorable.PickUpNumberSettingData[level.Id.IntegerValue]?.IsPickUpNumberSetting ?? false ;
      var routes = pickUpModels.Select( x => x.RouteName ).Where( r => r != "" ).Distinct() ;
      var seenTextNotePickUps = new List<TextNoteMapCreationModel>() ;
      var notSeenTextNotePickUps = new List<TextNoteMapCreationModel>() ;
      var textNotePickUpModels = new List<TextNoteMapCreationModel>() ;
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
        var conduitPickUpModel = pickUpModels
          .Where( p => p.RouteName == route && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() )
          .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } )
          .FirstOrDefault() ;
        if ( conduitPickUpModel == null ) continue ;

        ShowPickUp( document, routeCache, allConduitsOfRoutes, isDisplayPickUpNumber, conduitPickUpModel.PickUpModels,
          seenTextNotePickUps, notSeenTextNotePickUps, ref pickUpNumberOfPullBox ) ;
      }

      foreach ( var notSeenTextNotePickUp in notSeenTextNotePickUps ) {
        SetPositionForNotSeenTextNotePickUp( routeCache, allConduitsOfRoutes, notSeenTextNotePickUp ) ;
      }

      #region Set size of text note in entangled case

      textNotePickUpModels.AddRange( notSeenTextNotePickUps ) ;
      textNotePickUpModels.AddRange( seenTextNotePickUps ) ;
      var rooms = document.GetAllFamilyInstances( ElectricalRoutingFamilyType.Room ).ToList() ;
      var reSizeRooms = rooms.ToDictionary( x => x.Id.IntegerValue, _ => false ) ;
      foreach ( var textNotePickUpModel in textNotePickUpModels ) {
        if ( textNotePickUpModel.Position == null ) continue ;

        foreach ( var room in rooms ) {
          var isOutOfRoom = RoomRouteManager.CheckPickElementIsInOrOutRoom( room, textNotePickUpModel.Position ) ;
          if ( isOutOfRoom ) continue ;
          
          var reSizeRoomId = room.Id.IntegerValue ;

          if ( ! reSizeRooms[ reSizeRoomId ] && textNotePickUpModels.Any( x =>
              {
                if ( x.Position == null ) return false ;
                var xDistance = Math.Abs( x.Position.X - textNotePickUpModel.Position.X ) ;
                var yDistance = Math.Abs( x.Position.Y - textNotePickUpModel.Position.Y ) ;
                return xDistance < MaxDistanceBetweenTextNotes && yDistance < MaxDistanceBetweenTextNotes &&
                       xDistance >= MaxToleranceOfTextNotePosition && yDistance >= MaxToleranceOfTextNotePosition ;
              } ) )
            reSizeRooms[ reSizeRoomId ] = true ;

          textNotePickUpModel.RoomId = reSizeRoomId ;

          break ;
        }
      }

      foreach ( var textNotePickUpModel in textNotePickUpModels ) {
        if ( textNotePickUpModel.RoomId == null ) continue ;

        var textNote = CreateTextNote( document, textNotePickUpModel, reSizeRooms[ (int)textNotePickUpModel.RoomId ] ) ;
        if ( textNote != null )
          textNotePickUpModel.Id = textNote.UniqueId ;
      }

      #endregion

      SaveTextNotePickUpModel( textNotePickUpStorable, textNotePickUpModels.Select( x => new TextNotePickUpModel( x.Id, level.Name ) ).ToList() ) ;
    }
    
    private static void ShowPickUp(Document document, RouteCache routes, IReadOnlyDictionary<string, List<Conduit>> allConduitsOfRoutes, bool isDisplayPickUpNumber, 
      IEnumerable<PickUpModel> pickUpModels, List<TextNoteMapCreationModel> seenTextNotePickUps, List<TextNoteMapCreationModel> notSeenTextNotePickUps, ref int pickUpNumberOfPullBox )
    {
      var pickUpModelsGroupsByRouteNameRef = pickUpModels.GroupBy( p => p.RouteNameRef ) ;
      var notSeenQuantities = new Dictionary<string, double>() ;
      foreach ( var pickUpModelsGroup in pickUpModelsGroupsByRouteNameRef ) {
        var routeName = pickUpModelsGroup.Key ;
        var pickUpNumbers = PickUpManager.GetPickUpNumbersList( pickUpModelsGroup.AsEnumerable().ToList() ) ;
        var lastRoute = routes.LastOrDefault( r => r.Key == routeName ) ;
        var lastSegment = lastRoute.Value.RouteSegments.Last() ;
        
        double seenQuantity = 0 ;
        foreach ( var pickUpNumber in pickUpNumbers ) {
          var items = pickUpModelsGroup.AsEnumerable().Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
          
          foreach ( var item in items.Where( item => ! string.IsNullOrEmpty( item.Quantity ) ) ) {
            double.TryParse( item.Quantity, out var quantity ) ;
            if ( string.IsNullOrEmpty( item.Direction ) )
              seenQuantity += quantity ;
            else {
              if ( ! notSeenQuantities.Keys.Contains( item.Direction ) )
                notSeenQuantities.Add( item.Direction, 0 ) ;

              notSeenQuantities[ item.Direction ] += quantity ;
            }
          }

          // Seen quantity
          if ( ! allConduitsOfRoutes.ContainsKey( lastRoute.Key ) ) continue ;
          
          var conduitsOfRoute = allConduitsOfRoutes[lastRoute.Key] ;
          var toConnectorPosition = lastSegment.ToEndPoint.RoutingStartPosition ;

          var pullBoxUniqueId = PullBoxRouteManager.IsSegmentConnectedToPoPullBox( document, lastSegment ) ;
          var isToPullBox = ! string.IsNullOrEmpty( pullBoxUniqueId ) ;

          var conduitNearest = FindConduitNearest( document, conduitsOfRoute, toConnectorPosition, true ) ??
                               FindConduitNearest( document, conduitsOfRoute, toConnectorPosition ) ;
          if ( conduitNearest is { Location: LocationCurve location } ) {
            var line = ( location.Curve as Line )! ;
            var fromPoint = line.GetEndPoint( 0 ) ;
            var toPoint = line.GetEndPoint( 1 ) ;
            var direction = line.Direction ;
            var point = XyzUtil.GetMiddlePoint( fromPoint, toPoint, direction ) ;
            
            int counter;
            XYZ? position;
            var positionSeenTextNote = seenTextNotePickUps.Where( x => x.Position != null &&
              Math.Abs( x.PositionRef.X - point.X ) < MaxToleranceOfTextNotePosition && Math.Abs( x.PositionRef.Y - point.Y ) < MaxToleranceOfTextNotePosition )
              .OrderBy( x => x.Counter ).LastOrDefault();
            if ( positionSeenTextNote == default ) {
              counter = 1 ;
              position = point ;
            }
            else if ( ! isToPullBox ) {
              var isLeftOrTop = positionSeenTextNote.Counter % 2 != 0 ;
              
              if ( direction.Y is 1 or -1 )
                point = new XYZ( isLeftOrTop ? point.X + 1.7 + 1.5 * ( positionSeenTextNote.Counter - 1 ) / 2 : point.X - 1.5 * positionSeenTextNote.Counter / 2, point.Y, point.Z ) ;
              else if ( direction.X is 1 or -1 )
                point = new XYZ( point.X, isLeftOrTop ? point.Y - 1.7 - 1.5 * ( positionSeenTextNote.Counter - 1 ) / 2 : point.Y + 1.5 * positionSeenTextNote.Counter / 2, point.Z ) ;

              counter = positionSeenTextNote.Counter + 1 ;
              position = positionSeenTextNote.PositionRef ;
            }
            else continue ;

            var pickUpNumberStr = pickUpNumber ?? string.Empty ;
            if ( isToPullBox )
              pickUpNumberStr = ( ++pickUpNumberOfPullBox ).ToString() ;
              
            var textPickUpNumber = isDisplayPickUpNumber ? "[" + pickUpNumberStr + "]" : string.Empty ;
            var seenQuantityStr = textPickUpNumber + Math.Round( seenQuantity, 1 ) ;
            var pickUpAlignment = TextNotePickUpAlignment.Horizontal ;
            if ( direction is { Y: 1 or -1 } )
              pickUpAlignment = TextNotePickUpAlignment.Vertical ;
            var textNotePickUpModel = new TextNoteMapCreationModel( string.Empty, counter, position, point, seenQuantityStr, pickUpAlignment, null, null ) ;
            seenTextNotePickUps.Add( textNotePickUpModel ) ;
          }
        }
        
        // Not seen quantity
        foreach ( var notSeenQuantity in notSeenQuantities ) {
          var points = notSeenQuantity.Key.Split( ',' ) ;
          var xPoint = double.Parse( points.First() ) ;
          var yPoint = double.Parse( points.Skip( 1 ).First() ) ;

          if ( notSeenTextNotePickUps.Any( x => Math.Abs( x.PositionRef.X - xPoint ) < MaxToleranceOfTextNotePosition && Math.Abs( x.PositionRef.Y - yPoint ) < MaxToleranceOfTextNotePosition ) ) 
            continue ;

          var notSeenQuantityStr = "↓ " + Math.Round( notSeenQuantity.Value, 1 ) ;
          var txtPosition = new XYZ( xPoint, yPoint, 0 ) ;
          var textNotePickUpModel = new TextNoteMapCreationModel( string.Empty, 0, txtPosition, txtPosition, notSeenQuantityStr, TextNotePickUpAlignment.Oblique, null, null ) ;
          notSeenTextNotePickUps.Add( textNotePickUpModel );
        }
      }
    }
    
    private static void SetPositionForNotSeenTextNotePickUp( RouteCache routeCache, Dictionary<string, List<Conduit>> notSeenConduitsOfRoutes,
      TextNoteMapCreationModel notSeenTextNotePickUp )
    {
      var conduitDirections = new List<XYZ>() ;
      var textNotePositionRef = notSeenTextNotePickUp.PositionRef ;
      var textNotePosition = notSeenTextNotePickUp.Position ;

      foreach ( var route in routeCache ) {
        var routeSegments = route.Value.RouteSegments ;
        if ( ! notSeenConduitsOfRoutes.ContainsKey( route.Key ) ) continue ;
        
        var notSeenConduits = notSeenConduitsOfRoutes[ route.Key ] ;
        GetConduitDirectionsOfNotSeenTextNotePickUp( conduitDirections, routeSegments, textNotePositionRef, notSeenConduits, true ) ;
        GetConduitDirectionsOfNotSeenTextNotePickUp( conduitDirections, routeSegments, textNotePositionRef, notSeenConduits ) ;
      }

      var defaultDirections = new List<XYZ> { new(-1, 0, 0), new(1, 0, 0), new(0, -1, 0), new(0, 1, 0) } ;
      var textNoteDirection = defaultDirections.FirstOrDefault( d => ! conduitDirections.Any( cd => cd.IsAlmostEqualTo( d ) ) ) ;

      if ( textNoteDirection == null )
        textNotePosition = new XYZ( textNotePositionRef.X + 1, textNotePositionRef.Y + 2, textNotePositionRef.Z ) ;
      else if ( textNoteDirection.Y is 1 )
        textNotePosition = new XYZ( textNotePositionRef.X - 0.2, textNotePositionRef.Y + 1.8, textNotePositionRef.Z ) ;
      else if ( textNoteDirection.Y is -1 )
        textNotePosition = new XYZ( textNotePositionRef.X, textNotePositionRef.Y - 1.2, textNotePositionRef.Z ) ;
      else if ( textNoteDirection.X is 1 )
        textNotePosition = new XYZ( textNotePositionRef.X + 0.5, textNotePositionRef.Y, textNotePositionRef.Z ) ;
      else if ( textNoteDirection.X is -1 )
        textNotePosition = new XYZ( textNotePositionRef.X - 2.5, textNotePositionRef.Y, textNotePositionRef.Z ) ;
      
      notSeenTextNotePickUp.Position = textNotePosition ;
      notSeenTextNotePickUp.Direction = textNoteDirection ;
    }
    
    private static void GetConduitDirectionsOfNotSeenTextNotePickUp(List<XYZ> conduitDirections, IEnumerable<RouteSegment> routeSegments, XYZ notSeenTextNotePosition, List<Conduit> notSeenConduitsOfRoute, bool isFrom = false )
    {
      var routingStartPosition = isFrom ? routeSegments.First().FromEndPoint.RoutingStartPosition : routeSegments.Last().ToEndPoint.RoutingStartPosition ;

      if ( ! ( Math.Abs( routingStartPosition.X - notSeenTextNotePosition.X ) < MaxToleranceOfTextNotePosition ) ||
           ! ( Math.Abs( routingStartPosition.Y - notSeenTextNotePosition.Y ) < MaxToleranceOfTextNotePosition ) )
        return ;

      foreach ( var conduitOfRoute in notSeenConduitsOfRoute ) {
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

    private static Conduit? FindConduitNearest(Document document,List<Conduit> conduitsOfRoute, XYZ toPosition, bool isCheckLengthConduit = false)
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

    private static TextNote? CreateTextNote(Document doc, TextNoteMapCreationModel textNoteMapCreationModel, bool isSmallSize = false )
    {
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

      TextNoteOptions opts = new(textTypeId) { HorizontalAlignment = HorizontalTextAlignment.Center } ;
      
      if ( textNoteMapCreationModel.Position != null && isSmallSize ) {
        if ( textNoteMapCreationModel.PickUpAlignment is TextNotePickUpAlignment.Vertical )
          textNoteMapCreationModel.Position = new XYZ( textNoteMapCreationModel.Position.X + 0.7, textNoteMapCreationModel.Position.Y, textNoteMapCreationModel.Position.Z ) ;
        
        else if ( textNoteMapCreationModel.PickUpAlignment is TextNotePickUpAlignment.Oblique ) {
          var textNoteDirection = textNoteMapCreationModel.Direction ;
          
          if ( textNoteDirection == null )
            textNoteMapCreationModel.Position = new XYZ( textNoteMapCreationModel.Position.X, textNoteMapCreationModel.Position.Y, textNoteMapCreationModel.Position.Z ) ;
          
          else if ( textNoteDirection.Y is 1 )
            textNoteMapCreationModel.Position = new XYZ( textNoteMapCreationModel.Position.X, textNoteMapCreationModel.Position.Y - 0.8, textNoteMapCreationModel.Position.Z ) ;
          
          else if ( textNoteDirection.Y is -1 )
            textNoteMapCreationModel.Position = new XYZ( textNoteMapCreationModel.Position.X, textNoteMapCreationModel.Position.Y + 0.5, textNoteMapCreationModel.Position.Z ) ;
          
          else if ( textNoteDirection.X is 1 )
            textNoteMapCreationModel.Position = new XYZ( textNoteMapCreationModel.Position.X - 0.1, textNoteMapCreationModel.Position.Y, textNoteMapCreationModel.Position.Z ) ;
          
          else if ( textNoteDirection.X is -1 )
            textNoteMapCreationModel.Position = new XYZ( textNoteMapCreationModel.Position.X + 1.3, textNoteMapCreationModel.Position.Y, textNoteMapCreationModel.Position.Z ) ;
        }
        
        else
          textNoteMapCreationModel.Position = new XYZ( textNoteMapCreationModel.Position.X, textNoteMapCreationModel.Position.Y - 0.8, textNoteMapCreationModel.Position.Z ) ;
      }
      
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, textNoteMapCreationModel.Position, textNoteMapCreationModel.Content, opts ) ;

      if ( textNoteMapCreationModel.PickUpAlignment is TextNotePickUpAlignment.Oblique )
        ElementTransformUtils.RotateElement( doc, textNote.Id, Line.CreateBound( textNoteMapCreationModel.Position, textNoteMapCreationModel.Position + XYZ.BasisZ ),  Math.PI / 4 ) ;
      else if ( textNoteMapCreationModel.PickUpAlignment is TextNotePickUpAlignment.Vertical )
        ElementTransformUtils.RotateElement( doc, textNote.Id, Line.CreateBound( textNoteMapCreationModel.Position, textNoteMapCreationModel.Position + XYZ.BasisZ ),  Math.PI / 2 ) ;
      
      var color = new Color( 255, 225, 51 ) ;
      ConfirmUnsetCommandBase.ChangeElementColor( doc, new []{ textNote }, color ) ;

      return textNote ;
    }

    private static void SaveTextNotePickUpModel(TextNotePickUpModelStorable textNotePickUpStorable, IEnumerable<TextNotePickUpModel> textNotePickUpModels)
    {
      try {
        textNotePickUpStorable.TextNotePickUpData.AddRange(textNotePickUpModels) ;
        textNotePickUpStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
    
    public static Dictionary<string, int> GetPickUpNumberForConduitsToPullBox( Document document, List<PickUpModel> pickUpModelsByLevel )
    {
      var result = new Dictionary<string, int>() ;
      var pullBoxUniqueIds = new HashSet<string>() ;
      var routeCache = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var pickUpNumberOfPullBox = pickUpModelsByLevel.Where( x => !string.IsNullOrEmpty( x.PickUpNumber ) ).Max( x => Convert.ToInt32( x.PickUpNumber ) ) ;
      var routes = pickUpModelsByLevel.Select( x => x.RouteName ).Where( r => r != "" ).Distinct() ;
      foreach ( var route in routes ) {
        var conduitPickUpModel = pickUpModelsByLevel
          .Where( p => p.RouteName == route && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() )
          .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } )
          .FirstOrDefault() ;
        if ( conduitPickUpModel == null ) continue ;
    
        var pickUpModelsGroupsByRouteNameRef = conduitPickUpModel.PickUpModels.GroupBy( p => p.RouteNameRef ) ;
        foreach ( var pickUpModelsGroup in pickUpModelsGroupsByRouteNameRef ) {
          var routeName = pickUpModelsGroup.Key ;
          var lastRoute = routeCache.LastOrDefault( r => r.Key == routeName ) ;
          var lastSegment = lastRoute.Value.RouteSegments.Last() ;
          var pullBoxUniqueId = PullBoxRouteManager.IsSegmentConnectedToPoPullBox( document, lastSegment ) ;
          if ( string.IsNullOrEmpty( pullBoxUniqueId ) || pullBoxUniqueIds.Contains( pullBoxUniqueId ) ) continue ;
          
          pullBoxUniqueIds.Add( pullBoxUniqueId ) ;
          result.Add( routeName, ++pickUpNumberOfPullBox );
        }
      }
      
      return result; 
    }
  }
}