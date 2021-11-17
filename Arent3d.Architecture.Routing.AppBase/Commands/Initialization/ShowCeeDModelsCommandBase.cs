using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowCeeDModelsCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      var dlgCeeDModel = new CeeDModelDialog( doc ) ;

      dlgCeeDModel.ShowDialog() ;
      if ( dlgCeeDModel.DialogResult ?? false )
        return doc.Transaction( "TransactionName.Commands.Routing.PlacementSetCode".GetAppStringByKeyOrDefault( "Placement Set Code" ), _ =>
        {
          var uiDoc = commandData.Application.ActiveUIDocument ;
          var pickedObject = uiDoc.Selection.PickObject( ObjectType.Element ) ;

          var element = doc.GetElement( pickedObject.ElementId ) ;
          if ( null == element ) {
            MessageBox.Show( "Add Set Code text failed, because select connector failed" ) ;
            return Result.Cancelled ;
          }

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

          // opts.Rotation = Math.PI / 4;
          var (x, y, z) = ( element.Location as LocationPoint )!.Point ;
          var txtPosition = new XYZ( x - 2, y + 1.5, z ) ;
          TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, dlgCeeDModel.SelectedSetCode, opts ) ;

          return Result.Succeeded ;
        } ) ;
      return Result.Cancelled ;
    }
  }
}