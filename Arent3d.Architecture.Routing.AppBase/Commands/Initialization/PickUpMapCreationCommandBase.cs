using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class PickUpMapCreationCommandBase : IExternalCommand
  {
    private const double MaxToleranceOfTextNotePosition = 0.001 ;
    private const double AngleTolerance = 0.0001 ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      
      try {
        var result = document.Transaction( "TransactionName.Commands.Initialization.PickUpMapCreation".GetAppStringByKeyOrDefault( "Pick Up Map Creation" ), _ =>
        {
          var textNotePickUpStorable = document.GetTextNotePickUpStorable() ;
          var isDisplay = textNotePickUpStorable.TextNotePickUpData.Any() ;

          if ( ! isDisplay ) {
            var pickUpViewModel = new PickUpViewModel( document ) ;
            var pickUpModels = pickUpViewModel.DataPickUpModels ;
            if ( ! pickUpModels.Any() ) {
              MessageBox.Show( "Don't have pick up data.", "Message" ) ;
              return Result.Cancelled ;
            }
            
            ShowTextNotePickUp( textNotePickUpStorable, document, pickUpModels ) ;
          }
          else {
            RemoveTextNotePickUp( document ) ;
          }
        
          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    public static void ShowTextNotePickUp( TextNotePickUpModelStorable textNotePickUpStorable, Document document, List<PickUpModel> pickUpModels )
    {
      var isDisplayPickUpNumber = textNotePickUpStorable.IsPickUpNumberSetting ;
      var level = document.ActiveView.GenLevel.Name ;
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
          .Where( p => p.RouteName == route && p.Floor == level && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() )
          .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } )
          .FirstOrDefault() ;
        if ( conduitPickUpModel == null ) continue ;

        ShowPickUp( document, routeCache, allConduitsOfRoutes, isDisplayPickUpNumber, conduitPickUpModel.PickUpModels,
          seenTextNotePickUps, notSeenTextNotePickUps ) ;
      }

      foreach ( var notSeenTextNotePickUp in notSeenTextNotePickUps ) {
        SetPositionForNotSeenTextNotePickUp( document, routeCache, allConduitsOfRoutes, notSeenTextNotePickUp ) ;
      }

      #region Set size of text note in entangled case

      textNotePickUpModels.AddRange( notSeenTextNotePickUps ) ;
      textNotePickUpModels.AddRange( seenTextNotePickUps ) ;
      foreach ( var textNotePickUpModel in textNotePickUpModels ) {
        if ( textNotePickUpModel.TextNotePosition == null ) continue ;
        
        var textNote = document.GetAllElements<TextNote>().First( x => x.UniqueId == textNotePickUpModel.TextNoteId ) ;
        var angle = textNote.BaseDirection.AngleTo( new XYZ( 1, 0, 0 ) ) ;

        if ( ! textNotePickUpModels.Any( x =>
            {
              if ( x.TextNoteId == textNotePickUpModel.TextNoteId || x.TextNotePosition == null ) return false ;
              var xDistance = Math.Abs( x.TextNotePosition.X - textNotePickUpModel.TextNotePosition.X ) ;
              var yDistance = Math.Abs( x.TextNotePosition.Y - textNotePickUpModel.TextNotePosition.Y ) ;
              return xDistance < 1.5 && yDistance < 1.5 ;
            } ) ) continue ;

        var newTextNoteType = textNote.TextNoteType.Duplicate( textNote.TextNoteType.Name ) ;
        const BuiltInParameter paraIndex = BuiltInParameter.TEXT_SIZE ;
        var textSize = newTextNoteType.get_Parameter( paraIndex ) ;
        textSize.Set( textSize.AsDouble() / 2 ) ;
        textNote.ChangeTypeId( newTextNoteType.Id ) ;

        if ( Math.Abs( angle - Math.PI / 2 ) < AngleTolerance )
          textNote.Coord = new XYZ( textNote.Coord.X + 0.7, textNote.Coord.Y, textNote.Coord.Z ) ;
        else if ( Math.Abs( angle - Math.PI / 4 ) < AngleTolerance ) {
          var textNoteDirection = textNotePickUpModel.TextNoteDirection ;
          if ( textNoteDirection == null )
            textNote.Coord = new XYZ( textNote.Coord.X, textNote.Coord.Y, textNote.Coord.Z ) ;
          else if ( textNoteDirection.Y is 1 )
            textNote.Coord = new XYZ( textNote.Coord.X, textNote.Coord.Y - 0.8, textNote.Coord.Z ) ;
          else if ( textNoteDirection.Y is -1 )
            textNote.Coord = new XYZ( textNote.Coord.X, textNote.Coord.Y + 0.5, textNote.Coord.Z ) ;
          else if ( textNoteDirection.X is 1 )
            textNote.Coord = new XYZ( textNote.Coord.X - 0.1, textNote.Coord.Y, textNote.Coord.Z ) ;
          else if ( textNoteDirection.X is -1 )
            textNote.Coord = new XYZ( textNote.Coord.X + 1.3, textNote.Coord.Y, textNote.Coord.Z ) ;
        }
        else
          textNote.Coord = new XYZ( textNote.Coord.X, textNote.Coord.Y - 0.8, textNote.Coord.Z ) ;
      }

      #endregion

      SaveTextNotePickUpModel( document, textNotePickUpModels.Select( x => new TextNotePickUpModel( x.TextNoteId ) ).ToList() ) ;
    }

    private static void SetPositionForNotSeenTextNotePickUp( Document document, RouteCache routeCache, Dictionary<string, List<Conduit>> notSeenConduitsOfRoutes,
      TextNoteMapCreationModel notSeenTextNotePickUp )
    {
      var conduitDirections = new List<XYZ>() ;
      var textNotePositionRef = notSeenTextNotePickUp.TextNotePositionRef ;
      var textNotePosition = notSeenTextNotePickUp.TextNotePosition ;

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

      var textNote = document.GetAllElements<TextNote>().First( x => x.UniqueId == notSeenTextNotePickUp.TextNoteId ) ;
      textNote.Coord = textNotePosition ;
      
      notSeenTextNotePickUp.TextNotePosition = textNotePosition ;
      notSeenTextNotePickUp.TextNoteDirection = textNoteDirection ;
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

    private static void ShowPickUp(Document document, RouteCache routes, IReadOnlyDictionary<string, List<Conduit>> allConduitsOfRoutes, bool isDisplayPickUpNumber, 
      List<PickUpModel> pickUpModels, List<TextNoteMapCreationModel> seenTextNotePickUps, List<TextNoteMapCreationModel> notSeenTextNotePickUps )
    {
      var pickUpNumbers = GetPickUpNumbersList( pickUpModels ) ;
      var pickUpModel = pickUpModels.First() ;
      var routeName = pickUpModel.RouteName ;
      var lastRoute = routes.LastOrDefault( r =>
      {
        if ( r.Key is not { } rName ) return false ;
        var routeNameArray = rName.Split( '_' ) ;
        var strRouteName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
        return strRouteName == routeName ;
      } ) ;
      var lastSegment = lastRoute.Value.RouteSegments.Last() ;
      
      double seenQuantity = 0 ;
      var notSeenQuantities = new Dictionary<string, double>() ;
      foreach ( var pickUpNumber in pickUpNumbers ) {
        var items = pickUpModels.Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
        
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

        // Not seen quantity
        foreach ( var notSeenQuantity in notSeenQuantities ) {
          var points = notSeenQuantity.Key.Split( ',' ) ;
          var xPoint = double.Parse( points.First() ) ;
          var yPoint = double.Parse( points.Skip( 1 ).First() ) ;

          if ( notSeenTextNotePickUps.Any( x => Math.Abs( x.TextNotePositionRef.X - xPoint ) < MaxToleranceOfTextNotePosition && Math.Abs( x.TextNotePositionRef.Y - yPoint ) < MaxToleranceOfTextNotePosition ) ) 
            continue ;

          var notSeenQuantityStr = "↓ " + Math.Round( notSeenQuantity.Value, 1 ) ;
          var txtPosition = new XYZ( xPoint, yPoint, 0 ) ;
          var textNote = CreateTextNote( document, txtPosition , notSeenQuantityStr, true ) ;
          var textNotePickUpModel = new TextNoteMapCreationModel( textNote.UniqueId, 0, txtPosition, txtPosition, null ) ;
          notSeenTextNotePickUps.Add( textNotePickUpModel );
        }

        // Seen quantity
        if ( ! allConduitsOfRoutes.ContainsKey( lastRoute.Key ) ) continue ;
        
        var conduitsOfRoute = allConduitsOfRoutes[lastRoute.Key] ;
        var toConnectorPosition = lastSegment.ToEndPoint.RoutingStartPosition ;

        var conduitNearest = FindConduitNearest( document, conduitsOfRoute, toConnectorPosition ) ;

        if ( conduitNearest is { Location: LocationCurve location } ) {
          var line = ( location.Curve as Line )! ;
          var fromPoint = line.GetEndPoint( 0 ) ;
          var toPoint = line.GetEndPoint( 1 ) ;
          var direction = line.Direction ;
          var point = GetMiddlePoint( fromPoint, toPoint, direction ) ;
          
          int counter;
          XYZ? position;
          var positionSeenTextNote = seenTextNotePickUps.Where( x => x.TextNotePosition != null &&
            Math.Abs( x.TextNotePositionRef.X - point.X ) < MaxToleranceOfTextNotePosition && Math.Abs( x.TextNotePositionRef.Y - point.Y ) < MaxToleranceOfTextNotePosition )
            .OrderBy( x => x.TextNoteCounter ).LastOrDefault();
          if ( positionSeenTextNote == default ) {
            counter = 1 ;
            position = point ;
          }
          else {
            var isLeftOrTop = positionSeenTextNote.TextNoteCounter % 2 != 0 ;
            if ( direction.Y is 1 or -1 )
              point = new XYZ( isLeftOrTop ? point.X + 1.7 + 1.5 * ( positionSeenTextNote.TextNoteCounter - 1 ) / 2 :  point.X - 1.5 * positionSeenTextNote.TextNoteCounter / 2, point.Y, point.Z ) ;
            else if ( direction.X is 1 or -1 )
              point = new XYZ( point.X, isLeftOrTop ? point.Y - 1.7 - 1.5 * ( positionSeenTextNote.TextNoteCounter - 1 ) / 2 : point.Y + 1.5 * positionSeenTextNote.TextNoteCounter / 2, point.Z ) ;

            counter = positionSeenTextNote.TextNoteCounter + 1 ;
            position = positionSeenTextNote.TextNotePositionRef ;
          }

          var textPickUpNumber = isDisplayPickUpNumber ? "[" + pickUpNumber + "]" : string.Empty ;
          var seenQuantityStr = textPickUpNumber + Math.Round( seenQuantity, 1 ) ;
          var textNote = CreateTextNote( document, point, seenQuantityStr, false, direction ) ;
          var textNotePickUpModel = new TextNoteMapCreationModel( textNote.UniqueId, counter, position, point, null ) ;
          seenTextNotePickUps.Add( textNotePickUpModel ) ;
        }
      }
    }

    private static Conduit? FindConduitNearest(Document document,List<Conduit> conduitsOfRoute, XYZ toPosition)
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
        if ( distance >= minDistance || lengthConduit <= 1.0 ) continue ;
        
        minDistance = distance ;
        result = conduitOfRoute ;
      }

      return result ;
    }

    private static XYZ GetMiddlePoint( XYZ fromPoint, XYZ toPoint, XYZ direction )
    {
      if(direction.Y is 1 or -1) return new XYZ( ( fromPoint.X + toPoint.X ) / 2 - 1.5 , ( fromPoint.Y + toPoint.Y ) / 2, fromPoint.Z ) ;
      
      if(direction.X is 1 or -1) return new XYZ( ( fromPoint.X + toPoint.X ) / 2, ( fromPoint.Y + toPoint.Y ) / 2 + 1.5, fromPoint.Z ) ;
      
      return new XYZ( ( fromPoint.X + toPoint.X ) / 2, ( fromPoint.Y + toPoint.Y ) / 2, fromPoint.Z ) ;
    }

    private static TextNote CreateTextNote(Document doc, XYZ txtPosition, string text, bool isOblique = false, XYZ? direction = null )
    {
      var textTypeId = TextNoteHelper.FindOrCreateTextNoteType( doc )!.Id ;
      TextNoteOptions opts = new(textTypeId) { HorizontalAlignment = HorizontalTextAlignment.Center } ;
      
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, text, opts ) ;
      var deviceSymbolTextNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( ShowCeedModelsCommandBase.DeviceSymbolTextNoteTypeName, tt.Name ) ) ;
      if ( deviceSymbolTextNoteType != null ) {
        deviceSymbolTextNoteType.get_Parameter( BuiltInParameter.LINE_COLOR ).Set( 0 ) ;
        deviceSymbolTextNoteType.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( 1 ) ;
        deviceSymbolTextNoteType.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;
        textNote.ChangeTypeId( deviceSymbolTextNoteType.Id ) ;
      }
      else {
        var textNoteType = textNote.TextNoteType ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( .01 ) ;
        textNoteType.get_Parameter( BuiltInParameter.LINE_COLOR ).Set( 0 ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( 1 ) ;
        textNoteType.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;
        textNote.ChangeTypeId( textNoteType.Id ) ;
      }

      if ( isOblique )
        ElementTransformUtils.RotateElement( doc, textNote.Id, Line.CreateBound( txtPosition, txtPosition + XYZ.BasisZ ),  Math.PI / 4 ) ;
      else if ( direction is { Y: 1 or -1 } )
        ElementTransformUtils.RotateElement( doc, textNote.Id, Line.CreateBound( txtPosition, txtPosition + XYZ.BasisZ ),  Math.PI / 2 ) ;
      
      var color = new Color( 255, 225, 51 ) ;
      ConfirmUnsetCommandBase.ChangeElementColor( doc, new []{ textNote }, color ) ;

      return textNote ;
    }

    private static void SaveTextNotePickUpModel(Document document, List<TextNotePickUpModel> textNotePickUpModels)
    {
      var textNotePickUpStorable = document.GetTextNotePickUpStorable() ;
      try {
        textNotePickUpStorable.TextNotePickUpData.AddRange(textNotePickUpModels) ;
        textNotePickUpStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
    
    public static void RemoveTextNotePickUp( Document document )
    {
      var textNotePickUpStorable = document.GetTextNotePickUpStorable() ;
      var textNotePickUpData = textNotePickUpStorable.TextNotePickUpData;
      var textNotes = document.GetAllElements<TextNote>() ;
      
      foreach ( var textNotePickUp in textNotePickUpData ) {
        var textNote = textNotes.FirstOrDefault( t => t.UniqueId == textNotePickUp.TextNoteId) ;
        if( textNote == null ) continue ;
        document.Delete( textNote.Id ) ;
      }

      textNotePickUpStorable.TextNotePickUpData = new List<TextNotePickUpModel>() ;
      textNotePickUpStorable.Save() ;
    }
    
    private static List<string> GetPickUpNumbersList( List<PickUpModel> pickUpModels )
    {
      var pickUpNumberList = new List<string>() ;
      
      foreach ( var pickUpModel in pickUpModels.Where( pickUpModel => ! pickUpNumberList.Contains( pickUpModel.PickUpNumber ) ) ) {
        pickUpNumberList.Add( pickUpModel.PickUpNumber ) ;
      }

      return pickUpNumberList ;
    }
  }
}