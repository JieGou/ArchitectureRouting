using System ;
using System.Collections.Generic;
using System.Linq ;
using System.Windows ;
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
      var document = data.GetDocument() ;
      var uiDocument = new UIDocument( document ) ;
      var selection = uiDocument.Selection ;

      if ( selection.GetElementIds().Count == 1 && data.GetModifiedElementIds().Count == 1 && document.GetElement( data.GetModifiedElementIds().First() ) is TextNote textNote ) {
        using var transaction = new Transaction( document ) ;
        transaction.Start( "Modify Double Border" ) ;
        
        var curveLoop = GeometryHelper.GetOutlineTextNote( textNote ) ;
        var borderUniqueIds = ( from curve in curveLoop select document.Create.NewDetailCurve( document.ActiveView, curve ) into detailLine select detailLine.UniqueId ).ToList() ;
        
        var borderTextNoteStorable = document.GetAllStorables<BorderTextNoteStorable>().FirstOrDefault() ?? document.GetBorderTextNoteStorable() ;
        var borderTextNoteData = borderTextNoteStorable.BorderTextNoteData.Where( x => document.GetElement( x.TextNoteUniqueId ) is TextNote ).ToList() ;
        if ( borderTextNoteData.SingleOrDefault( x => x.TextNoteUniqueId == textNote.UniqueId ) is { } borderTextNoteModel ) {
          var detailLines = borderTextNoteModel.BorderUniqueIds.Split( ',' ).Select( x => document.GetElement( x ) ).OfType<DetailLine>().EnumerateAll() ;
          if ( detailLines.Any() )
            document.Delete( detailLines.Select( x => x.Id ).ToList() ) ;
        
          borderTextNoteModel.BorderUniqueIds = string.Join(",", borderUniqueIds) ;
        }
        else {
          borderTextNoteModel = new BorderTextNoteModel( textNote.UniqueId, string.Join( ",", borderUniqueIds ) ) ;
          borderTextNoteData.Add(borderTextNoteModel);
        }

        borderTextNoteStorable.BorderTextNoteData = borderTextNoteData ;
        borderTextNoteStorable.Save();
        selection.SetElementIds(new List<ElementId>());

        transaction.Commit() ;
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