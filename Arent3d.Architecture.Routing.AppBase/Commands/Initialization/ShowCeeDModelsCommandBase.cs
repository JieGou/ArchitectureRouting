using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ShowCeeDModelsCommandBase : IExternalCommand
  {
    protected abstract RoutingFamilyType RoutingFamilyType { get ; }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      var dlgCeeDModel = new CeeDModelDialog( doc ) ;

      dlgCeeDModel.ShowDialog() ;
      if ( ! ( dlgCeeDModel.DialogResult ?? false ) ) return Result.Cancelled ;
      if ( ! string.IsNullOrEmpty( dlgCeeDModel.SelectedDeviceSymbol ) ) {
        return doc.Transaction( "TransactionName.Commands.Routing.PlacementDeviceSymbol".GetAppStringByKeyOrDefault( "Placement Device Symbol" ), _ =>
        {
          var uiDoc = commandData.Application.ActiveUIDocument ;

          var (originX, originY, originZ) = uiDoc.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;
          var level = uiDoc.ActiveView.GenLevel ;
          var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
          var element = GenerateConnector( uiDoc, originX, originY, heightOfConnector, level ) ;

          ElementId defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
          var noteWidth = .05 ;

          // make sure note width works for the text type
          var minWidth = TextElement.GetMinimumAllowedWidth( doc, defaultTextTypeId ) ;
          var maxWidth = TextElement.GetMaximumAllowedWidth( doc, defaultTextTypeId ) ;
          if ( noteWidth < minWidth ) {
            noteWidth = minWidth ;
          }
          else if ( noteWidth > maxWidth ) {
            noteWidth = maxWidth ;
          }

          TextNoteOptions opts = new(defaultTextTypeId) ;
          opts.HorizontalAlignment = HorizontalTextAlignment.Left ;

          var txtPosition = new XYZ( originX - 2, originY + 2, heightOfConnector ) ;
          var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, dlgCeeDModel.SelectedDeviceSymbol, opts ) ;

          // create group of selected element and new text note
          ICollection<ElementId> groupIds = new List<ElementId>() ;
          groupIds.Add( element.Id ) ;
          groupIds.Add( textNote.Id ) ;
          doc.Create.NewGroup( groupIds ) ;

          return Result.Succeeded ;
        } ) ;
      }
      return Result.Succeeded ;
    }

    private Element GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( RoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      return symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
    }
  }
}