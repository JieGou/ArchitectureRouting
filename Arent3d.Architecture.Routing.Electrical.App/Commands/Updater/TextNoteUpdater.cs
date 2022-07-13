using System ;
using System.Collections.Generic;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Extensions ;
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
        
        string? borderUniqueIds = null;
        TextNote? textNote ;

        if ( data.GetAddedElementIds().Count == 1 ) {
          textNote = GetTextNote( document, data.GetAddedElementIds().First(), DoubleBorderCommand.TextNoteTypeName ) ;
          if ( null != textNote ) {
            var curves = GetDoubleBorderTextNote( textNote ) ;
            borderUniqueIds = CreateDetailCurve(document, curves) ;
          }
          else {
            textNote = GetTextNote( document, data.GetAddedElementIds().First(), SingleBorderCommand.TextNoteTypeName ) ;
            if ( null != textNote ) {
              var curves = GetSingleBorderTextNote( textNote ) ;
              borderUniqueIds = CreateDetailCurve(document, curves) ;
            }
          }

          if ( null == borderUniqueIds || null == textNote ) 
            return ;
          
          var borderTextNoteModel = new BorderTextNoteModel { BorderUniqueIds = borderUniqueIds };
          textNote.SetData(borderTextNoteModel);
        }
        else if ( data.GetModifiedElementIds().Count == 1 && selection.GetElementIds().Count == 1 && data.GetModifiedElementIds().First() == selection.GetElementIds().First() ) {
          textNote = GetTextNote( document, data.GetModifiedElementIds().First(), DoubleBorderCommand.TextNoteTypeName ) ;
          if ( null != textNote ) {
            var curves = GetDoubleBorderTextNote( textNote ) ;
            borderUniqueIds = CreateDetailCurve(document, curves) ;
          }
          else {
            textNote = GetTextNote( document, data.GetModifiedElementIds().First(), SingleBorderCommand.TextNoteTypeName ) ;
            if ( null != textNote ) {
              var curves = GetSingleBorderTextNote( textNote ) ;
              borderUniqueIds = CreateDetailCurve(document, curves) ;
            }
          }

          if ( null != borderUniqueIds && null != textNote ) {
            if ( textNote.GetData<BorderTextNoteModel>() is { } borderTextNoteModel ) {
              var detailLines = borderTextNoteModel.BorderUniqueIds.Split( JoinSign ).Select( x => document.GetElement( new ElementId(int.Parse(x)) ) ).OfType<DetailLine>().EnumerateAll() ;
              if ( detailLines.Any() )
                document.Delete( detailLines.Select( x => x.Id ).ToList() ) ;

              borderTextNoteModel.BorderUniqueIds = borderUniqueIds ;
            }
            else {
              borderTextNoteModel = new BorderTextNoteModel { BorderUniqueIds = borderUniqueIds } ;
            }
            textNote.SetData(borderTextNoteModel);
          }
          else {
            textNote = (TextNote) document.GetElement( data.GetModifiedElementIds().First() ) ;
            if ( textNote.GetData<BorderTextNoteModel>() is { } borderTextNoteModel ) {
              var detailLines = borderTextNoteModel.BorderUniqueIds.Split( JoinSign ).Select( x => document.GetElement( new ElementId(int.Parse(x)) ) ).OfType<DetailLine>().EnumerateAll() ;
              if ( detailLines.Any() )
                document.Delete( detailLines.Select( x => x.Id ).ToList() ) ;
            }
            textNote.DeleteData<BorderTextNoteModel>();
          }
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

    private string CreateDetailCurve( Document document, IEnumerable<Curve> curves )
    {
      var curveIds = new List<string>() ;
      var graphicStyle = document.Settings.Categories.get_Item( BuiltInCategory.OST_CurvesMediumLines ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      foreach ( var curve in curves ) {
        var dl = document.Create.NewDetailCurve( document.ActiveView, curve ) ;
        dl.LineStyle = graphicStyle;
        curveIds.Add($"{dl.Id.IntegerValue}");
      }
      return string.Join($"{JoinSign}", curveIds) ;
    }
  }
}