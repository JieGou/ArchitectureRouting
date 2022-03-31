using System ;
using System.Collections.Generic;
using System.Linq ;
using System.Windows ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Updater
{
  public static class TextNoteArent
  {
    public static bool Clicked;
    public const string ArentTextNoteType = "ArrentTextNoteType";
    public static Dictionary<ElementId, List<ElementId>> StorageLines = new();
    
    public static void CreateSingleBoxText(TextNote text )
    {
      var document = text.Document ;
      var bb = text.get_BoundingBox( document.ActiveView ) ;
      var min = bb.Min ;
      var max = bb.Max ;

      var line1 = Line.CreateBound( min, new XYZ( max.X, min.Y, min.Z ) ) ;
      var line2 = Line.CreateBound(new XYZ( max.X, min.Y, min.Z ), max ) ;
      var line3 = Line.CreateBound( max, new XYZ( min.X, max.Y, min.Z ) ) ;
      var line4 = Line.CreateBound( new XYZ( min.X, max.Y, min.Z ), min ) ;

      var curs = new CurveArray() ;
      curs.Append( line1 );
      curs.Append( line2 );
      curs.Append( line3 );
      curs.Append( line4 );
      
      var curveArray = document.Create.NewDetailCurveArray( document.ActiveView, curs) ;
      var listCurves = (from DetailCurve curve in curveArray select curve.Id).ToList();
      StorageLines[text.Id] = listCurves;
    }

    public static bool CheckIdIsDeleted(Document doc, ElementId id)
    {
      return doc.GetElement(id) != null;
    }
  }
  public class TextNoteUpdaterChanged : IUpdater
  {
    private static UpdaterId? _updaterId ;

    public TextNoteUpdaterChanged( AddInId? id )
    {
      _updaterId = new UpdaterId( id, new Guid( "92E18EDD-001E-4BA5-9764-38FA12A5DD94" ) ) ;
    }

    public void Execute( UpdaterData data )
    {
      var doc = data.GetDocument() ;
      var modifiedId = data.GetModifiedElementIds().FirstOrDefault() ;
      if(modifiedId is null) return;
      if(doc.GetElement(modifiedId) is not TextNote text) return;
      if(text.TextNoteType.Name != TextNoteArent.ArentTextNoteType) return;

      doc.Delete( TextNoteArent.StorageLines[text.Id].Where(x=>TextNoteArent.CheckIdIsDeleted(doc, x)).ToList() ) ;
      TextNoteArent.CreateSingleBoxText( text ) ;

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
      return "TextNoteChangeUpdater" ;
    }

    public string GetAdditionalInformation()
    {
      return "Arent, " + "https://arent3d.com" ;
    }

    public bool Register()
    {
      var textFilter = new ElementClassFilter(typeof(TextNote));

      UpdaterRegistry.RegisterUpdater( this ) ;
      UpdaterRegistry.AddTrigger( GetUpdaterId(), textFilter, Element.GetChangeTypeAny() ) ;
      return true ;
    }

    public void UnRegister()
    {
      if ( UpdaterRegistry.IsUpdaterRegistered( GetUpdaterId() ) ) UpdaterRegistry.UnregisterUpdater( GetUpdaterId() ) ;
    }

    public bool IsRegistered()
    {
      return UpdaterRegistry.IsUpdaterRegistered( GetUpdaterId() ) ;
    }
    
  }
  
  public class TextNoteUpdaterCreated : IUpdater
  {
    private static UpdaterId? _updaterId ;
    private readonly TextNoteType _textNoteType ;
    
    public TextNoteUpdaterCreated( AddInId? id, TextNoteType textNoteType )
    {
      _updaterId = new UpdaterId( id, new Guid( "C96F9CAC-81E4-4A8B-9857-90829C830DE5" ) ) ;
      _textNoteType = textNoteType;
    }

    public void Execute( UpdaterData data )
    {
      var doc = data.GetDocument() ;
      var addedElementIds = data.GetAddedElementIds() ;
      try {
        if (TextNoteArent.Clicked)
        {
          addedElementIds.ForEach( x =>
          {
            if (doc.GetElement(x) is TextNote text)
            {
              text.TextNoteType = _textNoteType;
              TextNoteArent.CreateSingleBoxText( text ) ;
            }
          } );
          
          TextNoteArent.Clicked = false;
        }
      }
      catch ( Exception e ) {
        MessageBox.Show( e.Message ) ;
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
      return "TextNoteCreateUpdater" ;
    }

    public string GetAdditionalInformation()
    {
      return "Arent, " + "https://arent3d.com" ;
    }

    public bool Register()
    {
      var textFilter = new ElementClassFilter(typeof(TextNote));

      UpdaterRegistry.RegisterUpdater( this ) ;
      UpdaterRegistry.AddTrigger( GetUpdaterId(), textFilter, Element.GetChangeTypeElementAddition() ) ;
      return true ;
    }

    public void UnRegister()
    {
      if ( UpdaterRegistry.IsUpdaterRegistered( GetUpdaterId() ) ) UpdaterRegistry.UnregisterUpdater( GetUpdaterId() ) ;
    }

    public bool IsRegistered()
    {
      return UpdaterRegistry.IsUpdaterRegistered( GetUpdaterId() ) ;
    }
    
   
    
  }
}