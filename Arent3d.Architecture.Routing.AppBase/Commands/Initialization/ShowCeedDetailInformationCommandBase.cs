using System ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ShowCeedDetailInformationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;
      string pickedText = "" ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      TextNotePickFilter textNoteFilter = new TextNotePickFilter() ;
      try {
        var element = uiDoc.Selection.PickObject( ObjectType.Element, textNoteFilter ) ;
        var textNote = doc.GetAllElements<TextNote>().ToList().FirstOrDefault( x => x.Id == element.ElementId ) ;
        if ( textNote != null ) {
          if ( textNote.GroupId != ElementId.InvalidElementId ) {
            var groupId = doc.GetAllElements<Group>().FirstOrDefault( g => g.Id == textNote.GroupId )?.AttachedParentId ;
            if ( groupId != null && groupId != ElementId.InvalidElementId ) {
              var connector = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).FirstOrDefault( e => e.GroupId == groupId || e.GroupId == textNote.GroupId ) ;
              if ( connector != null ) {
                connector.TryGetProperty( ConnectorFamilyParameter.CeedCode, out string? ceedSetCodeModel ) ;
                if ( ! string.IsNullOrEmpty( ceedSetCodeModel ) ) {
                  var ceedSetCode = ceedSetCodeModel!.Split( '-' ).ToList() ;
                  pickedText = ceedSetCode.FirstOrDefault() ?? string.Empty ;
                }
              }
            }
          }
        }
      }
      catch {
        return Result.Cancelled ;
      }

      if ( string.IsNullOrEmpty( pickedText ) ) return Result.Cancelled ;
      var dialog = new CeedDetailInformationDialog( doc, pickedText ) ;
      dialog.ShowDialog() ;

      if ( dialog.DialogResult ?? false ) {
        return Result.Succeeded ;
      }
      else {
        return Result.Cancelled ;
      }
    }

    private class TextNotePickFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return ( e.GetBuiltInCategory() == BuiltInCategory.OST_TextNotes ) ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return false ;
      }
    }
  }
}