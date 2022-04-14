using System ;
using System.Collections.Generic;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Updater
{
  public class TextNoteUpdater : IUpdater
  {
    private static UpdaterId? _updaterId ;

    public TextNoteUpdater( AddInId? id )
    {
      _updaterId = new UpdaterId( id, new Guid( "92E18EDD-001E-4BA5-9764-38FA12A5DD94" ) ) ;
    }

    public void Execute( UpdaterData data )
    {
      try {
        var document = data.GetDocument() ;
        var uiDocument = new UIDocument( document ) ;
        var selection = uiDocument.Selection ;

        if ( data.GetAddedElementIds().Count == 1 ) {
          var textNote = GetTextNote( document, data.GetAddedElementIds().First(), DoubleBorderCommand.TextNoteTypeName ) ;
          if ( null == textNote )
            return ;
          
          var curveLoop = GeometryHelper.GetOutlineTextNote( textNote ) ;
          curveLoop = CurveLoop.CreateViaOffset(curveLoop, -0.5.MillimetersToRevitUnits() * document.ActiveView.Scale, document.ActiveView.ViewDirection);
          var borderIds = ( from curve in curveLoop select document.Create.NewDetailCurve( document.ActiveView, curve ) into detailLine select $"{detailLine.Id.IntegerValue}" ).ToList() ;

          var borderTextNoteStorable = document.GetAllStorables<BorderTextNoteStorable>().FirstOrDefault() ?? document.GetBorderTextNoteStorable() ;
          borderTextNoteStorable.BorderTextNoteData.Add( new BorderTextNoteModel( textNote.Id.IntegerValue, string.Join( ",", borderIds ) ) ) ;
          borderTextNoteStorable.Save() ;
        }
        else if ( data.GetModifiedElementIds().Count == 1 && selection.GetElementIds().Count == 1 && data.GetModifiedElementIds().First() == selection.GetElementIds().First() ) {
          var textNote = GetTextNote( document, data.GetModifiedElementIds().First(), DoubleBorderCommand.TextNoteTypeName ) ;
          if ( null == textNote )
            return ;

          var curveLoop = GeometryHelper.GetOutlineTextNote( textNote ) ;
          curveLoop = CurveLoop.CreateViaOffset(curveLoop, -0.5.MillimetersToRevitUnits() * document.ActiveView.Scale, document.ActiveView.ViewDirection);
          var borderUniqueIds = ( from curve in curveLoop select document.Create.NewDetailCurve( document.ActiveView, curve ) into detailLine select $"{detailLine.Id.IntegerValue}" ).ToList() ;

          var borderTextNoteStorable = document.GetAllStorables<BorderTextNoteStorable>().FirstOrDefault() ?? document.GetBorderTextNoteStorable() ;
          if ( borderTextNoteStorable.BorderTextNoteData.SingleOrDefault( x => x.TextNoteId == textNote.Id.IntegerValue ) is { } borderTextNoteModel ) {
            var detailLines = borderTextNoteModel.BorderIds.Split( ',' ).Select( x => document.GetElement( new ElementId(int.Parse(x)) ) ).OfType<DetailLine>().EnumerateAll() ;
            if ( detailLines.Any() )
              document.Delete( detailLines.Select( x => x.Id ).ToList() ) ;

            borderTextNoteModel.BorderIds = string.Join( ",", borderUniqueIds ) ;
          }
          else {
            borderTextNoteModel = new BorderTextNoteModel( textNote.Id.IntegerValue, string.Join( ",", borderUniqueIds ) ) ;
            borderTextNoteStorable.BorderTextNoteData.Add( borderTextNoteModel ) ;
          }
          
          borderTextNoteStorable.Save() ;
          selection.SetElementIds( new List<ElementId>() ) ;
        }
        else if ( data.GetDeletedElementIds().Count == 1 ) {
          var borderTextNoteStorable = document.GetAllStorables<BorderTextNoteStorable>().FirstOrDefault() ?? document.GetBorderTextNoteStorable() ;
          if ( borderTextNoteStorable.BorderTextNoteData.SingleOrDefault( x => x.TextNoteId == data.GetDeletedElementIds().First().IntegerValue ) is { } borderTextNoteModel ) {
            var detailLines = borderTextNoteModel.BorderIds.Split( ',' ).Select( x => document.GetElement( new ElementId(int.Parse(x)) ) ).OfType<DetailLine>().EnumerateAll() ;
            if ( detailLines.Any() )
              document.Delete( detailLines.Select( x => x.Id ).ToList() ) ;
            
            borderTextNoteStorable.BorderTextNoteData.Remove( borderTextNoteModel ) ;
          }

          borderTextNoteStorable.Save() ;
        }
      }
      catch ( Exception exception ) {
        TaskDialog.Show( "Arent Inc", exception.Message ) ;
      }
    }

    public UpdaterId? GetUpdaterId()
    {
      return _updaterId ;
    }

    public ChangePriority GetChangePriority()
    {
      return ChangePriority.Annotations ;
    }

    public string GetUpdaterName()
    {
      return "Text Note Arent Updater" ;
    }

    public string GetAdditionalInformation()
    {
      return "Arent, " + "https://arent3d.com" ;
    }

    private static TextNote? GetTextNote(Document document, ElementId elementId, string textNoteTypeName )
    {
      if(document.GetElement(elementId) is not TextNote textNote)
        return null;
        
      if(document.GetElement(textNote.GetTypeId()) is not TextNoteType textNoteType)
        return null;
        
      return textNoteType.Name != textNoteTypeName ? null : textNote ;
    }
  }
}