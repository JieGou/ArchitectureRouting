using System ;
using System.Collections.Generic;
using System.Linq ;
using System.Windows ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Org.BouncyCastle.Asn1.Cms ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Updater
{
  public class TextNoteUpdaterChanged : IUpdater
  {
    private static UpdaterId? _updaterId ;
    private readonly bool _isDoubleBorder ;
    public TextNoteUpdaterChanged( AddInId? id, bool isDoubleBorder )
    {
      _isDoubleBorder = isDoubleBorder ;
      _updaterId = new UpdaterId( id, Guid.NewGuid() ) ;
    }

    public void Execute( UpdaterData data )
    {
      var doc = data.GetDocument() ;
      var modifiedId = data.GetModifiedElementIds().FirstOrDefault() ;
      if(modifiedId is null) return;
      if(doc.GetElement(modifiedId) is not TextNote text) return;
      if(text.TextNoteType.Name != ArentTextNote.ArentTextNoteType) return;
      doc.Delete( ArentTextNote.StorageLines[text.Id].Where(x=>ArentTextNote.CheckIdIsDeleted(doc, x)).ToList() ) ;
      if (_isDoubleBorder)
        ArentTextNote.CreateDoubleBorderText( text ) ;
      else
        ArentTextNote.CreateSingleBorderText( text ) ;
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
    private readonly bool _isDoubleBorder ;
    
    public TextNoteUpdaterCreated( AddInId? id, TextNoteType textNoteType, bool isDoubleBorder )
    {
      _updaterId = new UpdaterId( id, Guid.NewGuid() ) ;
      _textNoteType = textNoteType;
      _isDoubleBorder = isDoubleBorder ;
    }

    public void Execute( UpdaterData data )
    {
      var doc = data.GetDocument() ;
      var addedElementIds = data.GetAddedElementIds() ;
      try {
        if (ArentTextNote.Clicked)
        {
          addedElementIds.ForEach( x =>
          {
            if (doc.GetElement(x) is TextNote text)
            {
              text.TextNoteType = _textNoteType;
              if (_isDoubleBorder)
                ArentTextNote.CreateDoubleBorderText( text ) ;
              else
                ArentTextNote.CreateSingleBorderText( text ) ;
            }
          } );
          
          ArentTextNote.Clicked = false;
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