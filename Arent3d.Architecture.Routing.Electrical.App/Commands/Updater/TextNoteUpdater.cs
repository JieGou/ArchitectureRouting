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
    private readonly UpdaterId? _updaterId ;
    private const char JoinSign = ',' ;

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
        
        List<string>? borderIds = null;
        ElementId? textNoteId = null ;

        if ( data.GetAddedElementIds().Count == 1 ) {
          var doubleTextNote = GetTextNote( document, data.GetAddedElementIds().First(), DoubleBorderCommand.TextNoteTypeName ) ;
          if ( null != doubleTextNote ) {
            textNoteId = doubleTextNote.Id ;
            var curves = GetDoubleBorderTextNote( doubleTextNote ) ;
            borderIds = CreateDetailCurve(document, curves) ;
          }

          var singleTextNote = GetTextNote( document, data.GetAddedElementIds().First(), SingleBorderCommand.TextNoteTypeName ) ;
          if ( null != singleTextNote ) {
            textNoteId = singleTextNote.Id ;
            var curves = GetSingleBorderTextNote( singleTextNote ) ;
            borderIds = CreateDetailCurve(document, curves) ;
          }

          if ( null == borderIds || null == textNoteId ) 
            return ;
          
          var borderTextNoteStorable = document.GetAllStorables<BorderTextNoteStorable>().FirstOrDefault() ?? document.GetBorderTextNoteStorable() ;
          borderTextNoteStorable.BorderTextNoteData.Add( new BorderTextNoteModel( textNoteId.IntegerValue, string.Join( $"{JoinSign}", borderIds ) ) ) ;
          borderTextNoteStorable.Save() ;
        }
        else if ( data.GetModifiedElementIds().Count == 1 && selection.GetElementIds().Count == 1 && data.GetModifiedElementIds().First() == selection.GetElementIds().First() ) {
          var borderTextNoteStorable = document.GetAllStorables<BorderTextNoteStorable>().FirstOrDefault() ?? document.GetBorderTextNoteStorable() ;
          
          var doubleTextNote = GetTextNote( document, data.GetModifiedElementIds().First(), DoubleBorderCommand.TextNoteTypeName ) ;
          if ( null != doubleTextNote ) {
            textNoteId = doubleTextNote.Id ;
            var curves = GetDoubleBorderTextNote( doubleTextNote ) ;
            borderIds = CreateDetailCurve(document, curves) ;
          }
          
          var singleTextNote = GetTextNote( document, data.GetModifiedElementIds().First(), SingleBorderCommand.TextNoteTypeName ) ;
          if ( null != singleTextNote ) {
            textNoteId = singleTextNote.Id ;
            var curves = GetSingleBorderTextNote( singleTextNote ) ;
            borderIds = CreateDetailCurve(document, curves) ;
          }

          if ( null != borderIds && null != textNoteId ) {
            if ( borderTextNoteStorable.BorderTextNoteData.SingleOrDefault( x => x.TextNoteId == textNoteId.IntegerValue ) is { } borderTextNoteModel ) {
              var detailLines = borderTextNoteModel.BorderIds.Split( JoinSign ).Select( x => document.GetElement( new ElementId(int.Parse(x)) ) ).OfType<DetailLine>().EnumerateAll() ;
              if ( detailLines.Any() )
                document.Delete( detailLines.Select( x => x.Id ).ToList() ) ;

              borderTextNoteModel.BorderIds = string.Join( $"{JoinSign}", borderIds ) ;
            }
            else {
              borderTextNoteModel = new BorderTextNoteModel( textNoteId.IntegerValue, string.Join( $"{JoinSign}", borderIds ) ) ;
              borderTextNoteStorable.BorderTextNoteData.Add( borderTextNoteModel ) ;
            }
          }
          else {
            textNoteId = document.GetElement( data.GetModifiedElementIds().First() ).Id ;
            if ( borderTextNoteStorable.BorderTextNoteData.SingleOrDefault( x => x.TextNoteId == textNoteId.IntegerValue ) is { } textNoteModel ) {
              var detailLines = textNoteModel.BorderIds.Split( JoinSign ).Select( x => document.GetElement( new ElementId(int.Parse(x)) ) ).OfType<DetailLine>().EnumerateAll() ;
              if ( detailLines.Any() )
                document.Delete( detailLines.Select( x => x.Id ).ToList() ) ;
          
              borderTextNoteStorable.BorderTextNoteData.Remove( textNoteModel ) ;
            }
          }

          borderTextNoteStorable.Save() ;
          selection.SetElementIds( new List<ElementId>() ) ;
        }
        else if ( data.GetDeletedElementIds().Count == 1 ) {
          var borderTextNoteStorable = document.GetAllStorables<BorderTextNoteStorable>().FirstOrDefault() ?? document.GetBorderTextNoteStorable() ;
          if ( borderTextNoteStorable.BorderTextNoteData.SingleOrDefault( x => x.TextNoteId == data.GetDeletedElementIds().First().IntegerValue ) is { } textNoteModel ) {
            var detailLines = textNoteModel.BorderIds.Split( JoinSign ).Select( x => document.GetElement( new ElementId(int.Parse(x)) ) ).OfType<DetailLine>().EnumerateAll() ;
            if ( detailLines.Any() )
              document.Delete( detailLines.Select( x => x.Id ).ToList() ) ;
            
            borderTextNoteStorable.BorderTextNoteData.Remove( textNoteModel ) ;
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

    private TextNote? GetTextNote(Document document, ElementId elementId, string textNoteTypeName )
    {
      if(document.GetElement(elementId) is not TextNote textNote)
        return null;
        
      if(document.GetElement(textNote.GetTypeId()) is not TextNoteType textNoteType)
        return null;
        
      return textNoteType.Name != textNoteTypeName ? null : textNote ;
    }

    private IEnumerable<Curve> GetSingleBorderTextNote( TextNote textNote )
    {
      var curveLoop = GeometryHelper.GetOutlineTextNote( textNote ) ;
      return curveLoop.OfType<Curve>().ToList() ;
    }

    private IEnumerable<Curve> GetDoubleBorderTextNote(TextNote textNote)
    {
      var curveLoop = GeometryHelper.GetOutlineTextNote( textNote ) ;
      var curves = curveLoop.OfType<Curve>().ToList() ;
      var curveLoopOffset = CurveLoop.CreateViaOffset(curveLoop, -0.5.MillimetersToRevitUnits() * textNote.Document.ActiveView.Scale, textNote.Document.ActiveView.ViewDirection);
      curves.AddRange(curveLoopOffset.OfType<Curve>());
      return curves ;
    }

    public List<string> CreateDetailCurve( Document document, IEnumerable<Curve> curves )
    {
      var linePattern = LinePatternElement.GetLinePatternElementByName( document, "<Medium Lines>" ) ;
      var curveIds = new List<string>() ;
      var graphicStyle = document.Settings.Categories.get_Item( BuiltInCategory.OST_CurvesMediumLines ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      foreach ( var curve in curves ) {
        var dl = document.Create.NewDetailCurve( document.ActiveView, curve ) ;
        dl.LineStyle = graphicStyle;
        curveIds.Add($"{dl.Id.IntegerValue}");
      }
      return curveIds ;
    }
  }
}