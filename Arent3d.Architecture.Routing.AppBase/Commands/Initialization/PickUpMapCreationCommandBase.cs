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
using Arent3d.Architecture.Routing.StorableCaches ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class PickUpMapCreationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      
      try {
        var result = document.Transaction( "TransactionName.Commands.Initialization.PickUpMapCreation".GetAppStringByKeyOrDefault( "Pick Up Map Creation" ), _ =>
        {
          var pickUpViewModel = new PickUpViewModel( document ) ;
          var pickUpModels = pickUpViewModel.DataPickUpModels ;
          var textNotePickUpStorable = document.GetTextNotePickUpStorable() ;
          var isDisplay = textNotePickUpStorable.TextNotePickUpData.Any() ;
          var isDisplayPickUpNumber = textNotePickUpStorable.IsPickUpNumberSetting ;
          
          if ( ! isDisplay ) {
            if ( ! pickUpModels.Any() ) {
              MessageBox.Show( "Don't have pick up data.", "Message" ) ;
              return Result.Cancelled ;
            }
            
            var level = document.ActiveView.GenLevel.Name ;
            var routes = pickUpModels.Select( x => x.RouteName ).Where(r=> r != "").Distinct() ;
            var textNotePositions = new List<XYZ>() ; 
            foreach ( var route in routes ) {
              var conduitPickUpModels = pickUpModels
                .Where( p => p.RouteName == route && p.Floor == level && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() )
                .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ).ToList() ;
              var textNoteIdsPickUpModels = ShowPickUp( document, isDisplayPickUpNumber, conduitPickUpModels.First().PickUpModels, textNotePositions ) ;
              SaveTextNotePickUpModel( document, textNoteIdsPickUpModels ) ;
            }
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
    
    private List<TextNotePickUpModel> ShowPickUp(Document document, bool isDisplayPickUpNumber ,List<PickUpModel> pickUpModels, List<XYZ> positionsTextNote)
    {
      var pickUpNumbers = GetPickUpNumbersList( pickUpModels ) ;
      var pickUpModel = pickUpModels.First() ;
      var routeName = pickUpModel.RouteName ;
      var textNoteIds = new List<TextNotePickUpModel>() ;

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var lastRoute = routes.LastOrDefault( r => r.Key is { } rName && rName.Contains( routeName ) ) ;
      var lastSegment = lastRoute.Value.RouteSegments.Last() ;
      double seenQuantity = 0 ;
      Dictionary<string, double> notSeenQuantities = new Dictionary<string, double>() ;
      foreach ( var pickUpNumber in pickUpNumbers ) {
        var items = pickUpModels.Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
        foreach ( var item in items.Where( item => ! string.IsNullOrEmpty( item.Quantity ) ) ) {
          double.TryParse( item.Quantity, out var quantity ) ;
          if ( ! string.IsNullOrEmpty( item.Direction ) ) {
            if ( ! notSeenQuantities.Keys.Contains( item.Direction ) ) {
              notSeenQuantities.Add( item.Direction, 0 ) ;
            }

            notSeenQuantities[ item.Direction ] += quantity ;
          }
          else
            seenQuantity += quantity ;
        }

        // Not seen quantity
        foreach ( var notSeenQuantity in notSeenQuantities ) {
          var points = notSeenQuantity.Key.Split( ',' ) ;
          var xPoint = double.Parse( points.First() ) ;
          var yPoint = double.Parse( points.Skip( 1 ).First() ) ;
          var notSeenQuantityStr = "↓ " + Math.Round( notSeenQuantity.Value, 1 ) ;

          string textNoteId = CreateTextNote( document, new XYZ( xPoint - 0.5, yPoint - 1.5, 0 ), notSeenQuantityStr, true ) ;
          textNoteIds.Add( new TextNotePickUpModel( textNoteId ) ) ;
        }

        // Seen quantity
        var allConduits = new FilteredElementCollector( document ).OfCategory( BuiltInCategory.OST_Conduit ).OfType<Conduit>() ;
        var conduitsOfRoute = allConduits.Where( conduit => conduit.GetRouteName() is { } rName && rName == lastRoute.Key ).ToList() ;

        var toConnectorPosition = lastSegment.ToEndPoint.RoutingStartPosition ;

        var conduitNearest = FindConduitNearest( document, conduitsOfRoute, toConnectorPosition ) ;

        if ( conduitNearest is { Location: LocationCurve location } ) {
          var line = ( location.Curve as Line )! ;
          var fromPoint = line.GetEndPoint( 0 ) ;
          var toPoint = line.GetEndPoint( 1 ) ;
          var direction = line.Direction ;
          var point = MiddlePoint( fromPoint, toPoint, direction ) ;
          while ( positionsTextNote.Any( x => Math.Abs( x.X - point.X ) == 0 && Math.Abs( x.Y - point.Y ) == 0 ) ) {
            if ( direction.Y is 1 or -1 ) {
              point = new XYZ( point.X, point.Y + 1.3, point.Z ) ;
            }

            if ( direction.X is 1 or -1 ) {
              point = new XYZ( point.X + 1.3, point.Y, point.Z ) ;
            }
          }
          positionsTextNote.Add( point ) ;
          var textPickUpNumber = isDisplayPickUpNumber ? "[" + pickUpNumber + "]" : string.Empty ;
          var seenQuantityStr = textPickUpNumber + Math.Round( seenQuantity, 1 ) ;
          var textNoteId = CreateTextNote( document, point, seenQuantityStr, false, direction ) ;
          textNoteIds.Add( new TextNotePickUpModel( textNoteId ) ) ;
        }
      }

      return textNoteIds ;
    }

    private Conduit? FindConduitNearest(Document document,List<Conduit> conduitsOfRoute, XYZ toPosition)
    {
      Conduit? result = null  ;
      
      double minDistance = Double.MaxValue ;
      foreach ( var conduitOfRoute in conduitsOfRoute ) {
        if ( conduitOfRoute.Location is LocationCurve conduitLocation ) {
          var line = ( conduitLocation.Curve as Line )! ;
          var direction = line.Direction ;
          var toPoint = line.GetEndPoint( 1 ) ;
          var distance = toPosition.DistanceTo( toPoint ) ;
          if ( direction.Z is 1 or -1 ) continue ;
          var lengthConduit = conduitOfRoute.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ).AsDouble() ;
          if ( distance < minDistance && lengthConduit > 1.0) {
            minDistance = distance ;
            result = conduitOfRoute ;
          }
        }
      }

      return result ;
    }

    private XYZ MiddlePoint( XYZ fromPoint, XYZ toPoint, XYZ direction )
    {
      if(direction.Y is 1 or -1) return new XYZ( ( fromPoint.X + toPoint.X ) / 2 - 1.5 , ( fromPoint.Y + toPoint.Y ) / 2 - 1, fromPoint.Z ) ;
      
      if(direction.X is 1 or -1) return new XYZ( ( fromPoint.X + toPoint.X ) / 2 - 1, ( fromPoint.Y + toPoint.Y ) / 2 + 1.5, fromPoint.Z ) ;
      
      return new XYZ( ( fromPoint.X + toPoint.X ) / 2, ( fromPoint.Y + toPoint.Y ) / 2, fromPoint.Z ) ;
    }

    private string CreateTextNote(Document doc, XYZ txtPosition, string text, bool isRotate = false, XYZ? direction = null )
    {
      var textTypeId = TextNoteHelper.FindOrCreateTextNoteType( doc )!.Id ;
      TextNoteOptions opts = new(textTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left } ;
      
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

      if ( isRotate ) {
        ElementTransformUtils.RotateElement( doc, textNote.Id, Line.CreateBound( txtPosition, txtPosition + XYZ.BasisZ ),  Math.PI / 4 ) ;
      } else if ( direction is { Y: 1 or -1 } ) {
        ElementTransformUtils.RotateElement( doc, textNote.Id, Line.CreateBound( txtPosition, txtPosition + XYZ.BasisZ ),  Math.PI / 2 ) ;
      }
      
      var color = new Color( 255, 225, 51 ) ;
      ConfirmUnsetCommandBase.ChangeElementColor( doc, new []{ textNote }, color ) ;

      return textNote.UniqueId ;
    }

    private void SaveTextNotePickUpModel(Document document, List<TextNotePickUpModel> textNotePickUpModels)
    {
      var textNotePickUpStorable = document.GetTextNotePickUpStorable() ;
      try {
        textNotePickUpStorable.TextNotePickUpData.AddRange(textNotePickUpModels) ;
        textNotePickUpStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
    
    private void RemoveTextNotePickUp( Document document )
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
    
    private List<string> GetPickUpNumbersList( List<PickUpModel> pickUpModels )
    {
      var pickUpNumberList = new List<string>() ;
      foreach ( var pickUpModel in pickUpModels.Where( pickUpModel => ! pickUpNumberList.Contains( pickUpModel.PickUpNumber ) ) ) {
        pickUpNumberList.Add( pickUpModel.PickUpNumber ) ;
      }

      return pickUpNumberList ;
    }
  }
}