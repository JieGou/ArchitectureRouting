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
      var textNoteSeenPositions = new List<(string TextNoteId, int TextNoteCounter, XYZ TextNotePosition)>() ;
      var textNoteNotSeenPositions = new List<(string TextNoteId, int TextNoteCounter, XYZ TextNotePosition)>() ;
      var textNoteIdsPickUpModels = new List<TextNotePickUpModel>() ;
      var allConduits = new FilteredElementCollector( document ).OfCategory( BuiltInCategory.OST_Conduit ).OfType<Conduit>().ToList() ;
      var routeCache = RouteCache.Get( DocumentKey.Get( document ) ) ;

      foreach ( var route in routes ) {
        var conduitPickUpModel = pickUpModels
          .Where( p => p.RouteName == route && p.Floor == level && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() )
          .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } )
          .FirstOrDefault() ;
        if ( conduitPickUpModel == null ) continue ;
        
        textNoteIdsPickUpModels.AddRange( ShowPickUp( document, routeCache, allConduits, isDisplayPickUpNumber, conduitPickUpModel.PickUpModels, textNoteSeenPositions, textNoteNotSeenPositions ) ) ;
      }

      foreach ( var textNoteNotSeenPosition in textNoteNotSeenPositions ) {
        SetPositionForNotSeenTextNotePickUp( document, routeCache, allConduits, textNoteNotSeenPosition ) ;
      }

      #region Set size of text note in entangled case

      var textNotePositions = new List<(string TextNoteId, int TextNoteCounter, XYZ TextNotePosition)>() ;
      textNotePositions.AddRange( textNoteNotSeenPositions ) ;
      textNotePositions.AddRange( textNoteSeenPositions ) ;
      foreach ( var textNotePosition in textNotePositions ) {
        var textNote = document.GetAllElements<TextNote>().First( x => x.UniqueId == textNotePosition.TextNoteId ) ;
        var angle = textNote.BaseDirection.AngleTo( new XYZ( 1, 0, 0 ) ) ;
        var isRotated = angle != 0 ;

        if ( ! textNotePositions.Any( x =>
            {
              if ( x.TextNoteId == textNotePosition.TextNoteId ) return false ;
              var xDistance = Math.Abs( x.TextNotePosition.X - textNotePosition.TextNotePosition.X ) ;
              var yDistance = Math.Abs( x.TextNotePosition.Y - textNotePosition.TextNotePosition.Y ) ;
              if ( isRotated )
                return xDistance < 2 && yDistance < 2 ;
              return xDistance < 1.5 && yDistance < 1.5 ;
            } ) ) continue ;

        var newTextNoteType = textNote.TextNoteType.Duplicate( textNote.TextNoteType.Name ) ;
        const BuiltInParameter paraIndex = BuiltInParameter.TEXT_SIZE ;
        var textSize = newTextNoteType.get_Parameter( paraIndex ) ;
        textSize.Set( textSize.AsDouble() / 2 ) ;
        textNote.ChangeTypeId( newTextNoteType.Id ) ;

        if ( Math.Abs( angle - Math.PI / 2 ) < AngleTolerance )
          textNote.Coord = new XYZ( textNote.Coord.X + 0.7, textNote.Coord.Y, textNote.Coord.Z ) ;
        else if ( Math.Abs( angle - Math.PI / 4 ) < AngleTolerance )
          textNote.Coord = new XYZ( textNote.Coord.X + 0.7, textNote.Coord.Y - 0.3, textNote.Coord.Z ) ;
        else
          textNote.Coord = new XYZ( textNote.Coord.X, textNote.Coord.Y - 0.8, textNote.Coord.Z ) ;
      }

      #endregion

      SaveTextNotePickUpModel( document, textNoteIdsPickUpModels ) ;
    }

    private static void SetPositionForNotSeenTextNotePickUp( Document document, RouteCache routeCache, IReadOnlyCollection<Conduit> allConduits,
      (string TextNoteId, int TextNoteCounter, XYZ TextNotePosition) textNoteNotSeenPosition )
    {
      var conduitDirections = new List<XYZ>() ;
      var textNotePosition = textNoteNotSeenPosition.TextNotePosition ;

      foreach ( var route in routeCache ) {
        var routeSegments = route.Value.RouteSegments ;
        var notSeenConduitsOfRoute = allConduits.Where( conduit => conduit.GetRouteName() is { } rName && rName == route.Key ).ToList() ;

        conduitDirections.AddRange( GetConduitDirectionsOfNotSeenTextNotePickUp( routeSegments, textNotePosition, notSeenConduitsOfRoute, true ) ) ;
        conduitDirections.AddRange( GetConduitDirectionsOfNotSeenTextNotePickUp( routeSegments, textNotePosition, notSeenConduitsOfRoute ) ) ;
      }

      var defaultDirections = new List<XYZ> { new(0, 1, 0), new(1, 0, 0), new(-1, 0, 0), new(0, -1, 0) } ;
      var textNoteDirection = defaultDirections.FirstOrDefault( d => ! conduitDirections.Any( cd => cd.IsAlmostEqualTo( d ) ) ) ;

      if ( textNoteDirection == null )
        textNotePosition = new XYZ( textNotePosition.X + 1, textNotePosition.Y + 2, textNotePosition.Z ) ;
      else if ( textNoteDirection.Y is 1 )
        textNotePosition = new XYZ( textNotePosition.X, textNotePosition.Y + 2, textNotePosition.Z ) ;
      else if ( textNoteDirection.Y is -1 )
        textNotePosition = new XYZ( textNotePosition.X, textNotePosition.Y - 1.5, textNotePosition.Z ) ;
      else if ( textNoteDirection.X is 1 )
        textNotePosition = new XYZ( textNotePosition.X + 1, textNotePosition.Y, textNotePosition.Z ) ;
      else if ( textNoteDirection.X is -1 )
        textNotePosition = new XYZ( textNotePosition.X - 2.5, textNotePosition.Y, textNotePosition.Z ) ;

      var textNote = document.GetAllElements<TextNote>().First( x => x.UniqueId == textNoteNotSeenPosition.TextNoteId ) ;
      textNote.Coord = textNotePosition ;
    }

    private static IEnumerable<XYZ> GetConduitDirectionsOfNotSeenTextNotePickUp( IEnumerable<RouteSegment> routeSegments, XYZ notSeenTextNotePosition, List<Conduit> notSeenConduitsOfRoute, bool isFrom = false )
    {
      var conduitDirections = new List<XYZ>() ;
      var routingStartPosition = isFrom ? routeSegments.First().FromEndPoint.RoutingStartPosition : routeSegments.Last().ToEndPoint.RoutingStartPosition ;

      if ( ! ( Math.Abs( routingStartPosition.X - notSeenTextNotePosition.X ) < MaxToleranceOfTextNotePosition ) ||
           ! ( Math.Abs( routingStartPosition.Y - notSeenTextNotePosition.Y ) < MaxToleranceOfTextNotePosition ) )
        return conduitDirections ;

      foreach ( var conduitOfRoute in notSeenConduitsOfRoute ) {
        if ( conduitOfRoute.Location is not LocationCurve conduitLocation ) continue ;

        var line = ( conduitLocation.Curve as Line )! ;
        var direction = isFrom ? line.Direction : line.Direction.Negate() ;
        if ( direction.Z is 1 or -1 ) continue ;

        if( !conduitDirections.Any( cd => cd.IsAlmostEqualTo( direction )) )
          conduitDirections.Add( direction ) ;
      }

      return conduitDirections ;
    }

    private static IEnumerable<TextNotePickUpModel> ShowPickUp(Document document, RouteCache routes, IReadOnlyCollection<Conduit> allConduits, bool isDisplayPickUpNumber, 
      List<PickUpModel> pickUpModels, List<(string TextNoteId,int TextNoteCounter, XYZ TextNotePosition)> positionsSeenTextNote, List<(string TextNoteId, int TextNoteCounter, XYZ TextNotePosition)> positionsNotSeenTextNote )
    {
      var pickUpNumbers = GetPickUpNumbersList( pickUpModels ) ;
      var pickUpModel = pickUpModels.First() ;
      var routeName = pickUpModel.RouteName ;
      var textNoteIds = new List<TextNotePickUpModel>() ;
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

          if ( positionsNotSeenTextNote.Any( x => Math.Abs( x.TextNotePosition.X - xPoint ) < MaxToleranceOfTextNotePosition && Math.Abs( x.TextNotePosition.Y - yPoint ) < MaxToleranceOfTextNotePosition ) ) 
            continue ;

          var notSeenQuantityStr = "↓ " + Math.Round( notSeenQuantity.Value, 1 ) ;
          var txtPosition = new XYZ( xPoint, yPoint, 0 ) ;
          var textNote = CreateTextNote( document, txtPosition , notSeenQuantityStr, true ) ;
          textNoteIds.Add( new TextNotePickUpModel( textNote.UniqueId ) ) ;
          positionsNotSeenTextNote.Add( (textNote.UniqueId, 0, txtPosition) );
        }

        // Seen quantity
        var conduitsOfRoute = allConduits.Where( conduit => conduit.GetRouteName() is { } rName && rName == lastRoute.Key ).ToList() ;
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
          var positionSeenTextNote = positionsSeenTextNote.FirstOrDefault( x =>
            Math.Abs( x.TextNotePosition.X - point.X ) < MaxToleranceOfTextNotePosition && Math.Abs( x.TextNotePosition.Y - point.Y ) < MaxToleranceOfTextNotePosition ) ;
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
            position = positionSeenTextNote.TextNotePosition ;
            positionsSeenTextNote.Remove( positionSeenTextNote ) ;
          }

          var textPickUpNumber = isDisplayPickUpNumber ? "[" + pickUpNumber + "]" : string.Empty ;
          var seenQuantityStr = textPickUpNumber + Math.Round( seenQuantity, 1 ) ;
          var textNote = CreateTextNote( document, point, seenQuantityStr, false, direction ) ;
          positionsSeenTextNote.Add( (textNote.UniqueId, counter, position ) ) ;
          textNoteIds.Add( new TextNotePickUpModel( textNote.UniqueId ) ) ;
        }
      }

      return textNoteIds ;
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