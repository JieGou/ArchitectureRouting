using System ;
using System.Collections.Generic;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Updater
{
  public class TextNoteUpdater : IUpdater
  {
    private readonly UpdaterId? _updaterId ;

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

        var dataStorage = document.FindOrCreateDataStorageForUser() ;
        List<ElementId>? borderIds = null;
        TextNote? textNote ;

        if ( data.GetAddedElementIds().Count == 1 ) {
          textNote = GetTextNote( document, data.GetAddedElementIds().First(), DoubleBorderCommand.TextNoteTypeName ) ;
          if ( null != textNote ) {
            var curves = GetDoubleBorderTextNote( textNote ) ;
            borderIds = CreateDetailCurve(document, textNote, curves) ;
          }
          else {
            textNote = GetTextNote( document, data.GetAddedElementIds().First(), SingleBorderCommand.TextNoteTypeName ) ;
            if ( null != textNote ) {
              var curves = GetSingleBorderTextNote( textNote ) ;
              borderIds = CreateDetailCurve(document, textNote, curves) ;
            }
          }

          if ( null == borderIds || null == textNote ) 
            return ;

          SetDataForTextNote( dataStorage, textNote, borderIds ) ;
        }
        else if ( data.GetModifiedElementIds().Count == 1 && selection.GetElementIds().Count == 1 && data.GetModifiedElementIds().First() == selection.GetElementIds().First() ) {
          textNote = GetTextNote( document, data.GetModifiedElementIds().First(), DoubleBorderCommand.TextNoteTypeName ) ;
          if ( null != textNote ) {
            var curves = GetDoubleBorderTextNote( textNote ) ;
            borderIds = CreateDetailCurve(document, textNote, curves) ;
          }
          else {
            textNote = GetTextNote( document, data.GetModifiedElementIds().First(), SingleBorderCommand.TextNoteTypeName ) ;
            if ( null != textNote ) {
              var curves = GetSingleBorderTextNote( textNote ) ;
              borderIds = CreateDetailCurve(document, textNote, curves) ;
            }
          }

          if ( null != borderIds && null != textNote ) {
            SetDataForTextNote( dataStorage, textNote, borderIds ) ;
          }
          else {
            textNote = (TextNote) document.GetElement( data.GetModifiedElementIds().First() ) ;
            var borderTextNoteModel = dataStorage.GetData<BorderTextNoteModel>() ;
            if (  null != borderTextNoteModel && borderTextNoteModel.BorderTextNotes.ContainsKey(textNote.Id.IntegerValue)) {
              var detailLines = borderTextNoteModel.BorderTextNotes[ textNote.Id.IntegerValue ] .BorderIds.Where(x => x != ElementId.InvalidElementId).ToList();
              if ( detailLines.Any() )
                document.Delete( detailLines ) ;

              borderTextNoteModel.BorderTextNotes.Remove( textNote.Id.IntegerValue ) ;
            }
            
            if(null != borderTextNoteModel)
              dataStorage.SetData(borderTextNoteModel);
          }
        }
        else if ( data.GetDeletedElementIds().Count == 1 ) {
          var elementId = data.GetDeletedElementIds().First().IntegerValue ;
          var dataModel = dataStorage.GetData<BorderTextNoteModel>() ;
          if ( null == dataModel || ! dataModel.BorderTextNotes.ContainsKey( elementId ) || ! dataModel.BorderTextNotes[ elementId ].BorderIds.Any() )
            return ;
          
          var oldDetailCurveIds = dataModel.BorderTextNotes[ elementId ].BorderIds.Where( x => x != ElementId.InvalidElementId ).ToList() ;
          document.Delete( oldDetailCurveIds ) ;

          dataModel.BorderTextNotes.Remove( elementId ) ;
          dataStorage.SetData(dataModel);
        }
      }
      catch ( Exception exception ) {
        TaskDialog.Show( "Arent Inc", exception.Message ) ;
      }
    }

    private void SetDataForTextNote(DataStorage dataStorage, TextNote textNote, List<ElementId> newDetailLineIds )
    {
      if ( dataStorage.GetData<BorderTextNoteModel>() is not { } data )
        data = new BorderTextNoteModel() ;
      
      if ( data.BorderTextNotes.ContainsKey(textNote.Id.IntegerValue) ) {
        var oldDetailCurveIds = data.BorderTextNotes[textNote.Id.IntegerValue].BorderIds.Where( x => x != ElementId.InvalidElementId ).ToList() ;
        if( oldDetailCurveIds.Any() )
          dataStorage.Document.Delete( oldDetailCurveIds ) ;

        data.BorderTextNotes[ textNote.Id.IntegerValue ] = new BorderModel { BorderIds = newDetailLineIds } ;
        dataStorage.SetData(data);
      }
      else {
        data.BorderTextNotes.Add( textNote.Id.IntegerValue, new BorderModel { BorderIds = newDetailLineIds } );
        dataStorage.SetData(data);
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

    private List<ElementId> CreateDetailCurve( Document document, TextNote textNote, IEnumerable<Curve> curves )
    {
      var curveIds = new List<ElementId>() ;
      var graphicStyle = document.Settings.Categories.get_Item( BuiltInCategory.OST_CurvesMediumLines ).GetGraphicsStyle( GraphicsStyleType.Projection ) ;
      foreach ( var curve in curves ) {
        var dl = document.Create.NewDetailCurve( (View) document.GetElement( textNote.OwnerViewId ), curve ) ;
        dl.LineStyle = graphicStyle;
        curveIds.Add(dl.Id);
      }
      return curveIds ;
    }
  }
}