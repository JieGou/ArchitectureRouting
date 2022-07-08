using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
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

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class PickUpMapCreationCommandBase : IExternalCommand
  {
    protected abstract AddInType GetAddInType() ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var viewModel = new PickUpMapCreationViewModel( document ) ;
      var dialog = new PickUpMapCreationDialog( viewModel ) ;

      dialog.ShowDialog() ;
      if ( dialog.DialogResult == false ) return Result.Cancelled ;
      
      try {
        var result = document.Transaction( "TransactionName.Commands.Initialization.PickUpMapCreation".GetAppStringByKeyOrDefault( "Pick Up Map Creation" ), _ =>
        {
          if ( viewModel.IsDoconEnable ) {
            if ( ! viewModel.PickUpModels.Any() ) {
              MessageBox.Show( "Don't have pick up data.", "Message" ) ;
              return Result.Cancelled ;
            }

            RemoveTextNotePickUp( document ) ;
            
            var level = document.ActiveView.GenLevel.Name ;
            var routes = viewModel.PickUpModels.Select( x => x.RouteName ).Where(r=> r != "").Distinct() ;
            foreach ( var route in routes ) {
              var conduitPickUpModels = viewModel.PickUpModels
                .Where( p => p.RouteName == route && p.Floor == level && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() )
                .GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ).ToList() ;
              var textNoteIdsPickUpModels = ShowPickUp( document, viewModel, conduitPickUpModels.First().PickUpModels ) ;
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
    
    private List<TextNotePickUpModel> ShowPickUp(Document document, PickUpMapCreationViewModel viewModel , List<PickUpModel> pickUpModels )
    {
      var pickUpNumbers = viewModel.GetPickUpNumbersList( pickUpModels ) ;
      var pickUpModel = pickUpModels.First() ;
      var routeName = pickUpModel.RouteName ;
      var textNoteIds = new List<TextNotePickUpModel>() ;
      
      foreach ( var pickUpNumber in pickUpNumbers ) {
        double seenQuantity = 0 ;
        Dictionary<string, double> notSeenQuantities = new Dictionary<string, double>() ;
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
          var xPoint  = double.Parse(points.First()) ;
          var yPoint = double.Parse(points.Skip( 1 ).First()) ;
          var textPickUpNumber = viewModel.DoconTypes.First().TheValue ? "[" + pickUpNumber + "]" : string.Empty ;
          var notSeenQuantityStr = textPickUpNumber + "↓" + Math.Round( notSeenQuantity.Value, 1 ) ;
          
          string textNoteId = CreateTextNote( document, new XYZ( xPoint, yPoint, 0 ), notSeenQuantityStr, true );
          textNoteIds.Add( new TextNotePickUpModel(textNoteId) );
        }

        // Seen quantity
        var allConduits = new FilteredElementCollector( document ).OfCategory( BuiltInCategory.OST_Conduit ) ;
        var conduitsOfRoute = allConduits.Where( conduit => conduit.GetRouteName() == routeName ).ToList();
        var conduitMaxLength = conduitsOfRoute.Max(c => c.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ).AsDouble() );
        Element conduitHaveMaxLength = conduitsOfRoute.First(c=>  ( c.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ).AsDouble() - conduitMaxLength ) == 0 ) ;
        
        if ( conduitHaveMaxLength.Location is LocationCurve location ) {
          var line = ( location.Curve as Line )! ;
          var fromPoint = line.GetEndPoint( 0 ) ;
          var toPoint = line.GetEndPoint( 1 ) ;
          var direction = line.Direction ;
          var point = MiddlePoint( fromPoint, toPoint, direction ) ;
          var textPickUpNumber = viewModel.DoconTypes.First().TheValue ? "[" + pickUpNumber + "]" : string.Empty ;
          var seenQuantityStr = textPickUpNumber +  Math.Round( seenQuantity, 1 )  ;
          string textNoteId = CreateTextNote( document, point, seenQuantityStr, true ) ;
          textNoteIds.Add( new TextNotePickUpModel(textNoteId)  );
        }
      }

      return textNoteIds ;
    }

    private XYZ MiddlePoint( XYZ fromPoint, XYZ toPoint, XYZ direction )
    {
      if(direction.Y is 1 or -1) return new XYZ( (( fromPoint.X + toPoint.X ) / 2 ) + 0.4 , (( fromPoint.Y + toPoint.Y ) / 2 ), fromPoint.Z ) ;
      
      if(direction.X is 1 or -1) return new XYZ( (( fromPoint.X + toPoint.X ) / 2 ), (( fromPoint.Y + toPoint.Y ) / 2) + 0.5, fromPoint.Z ) ;
      
      return new XYZ( ( fromPoint.X + toPoint.X ) / 2, ( fromPoint.Y + toPoint.Y ) / 2, fromPoint.Z ) ;
    }

    private string CreateTextNote(Document doc, XYZ point, string text, bool isChangeColor )
    {
      var textTypeId = TextNoteHelper.FindOrCreateTextNoteType( doc )!.Id ;
      TextNoteOptions opts = new(textTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left } ;
      
      var txtPosition = new XYZ( point.X, point.Y, point.Z ) ;
      
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, text, opts ) ;

      var textNoteType = textNote.TextNoteType ;
      double newSize = ( 1.0 / 4.0 ) * TextNoteHelper.TextSize.MillimetersToRevitUnits() ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( newSize ) ;
      textNote.ChangeTypeId( textNoteType.Id ) ;

      if ( isChangeColor ) {
        var color = new Color( 255, 225, 0 ) ;
        ConfirmUnsetCommandBase.ChangeElementColor( doc, new []{ textNote }, color ) ;
      }

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
    }
  }
}