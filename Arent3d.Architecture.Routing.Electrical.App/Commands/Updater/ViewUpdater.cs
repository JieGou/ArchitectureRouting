using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Updater
{
public class ViewUpdater : IUpdater
  {
    private static UpdaterId? _updaterId ;

    public ViewUpdater( AddInId? id )
    {
      _updaterId = new UpdaterId( id, new Guid( "710A4FA7-D660-40EA-AC83-505D0A10199C" ) ) ;
    }

    public void Execute( UpdaterData data )
    {
      var document = data.GetDocument();
      if(document is null) return;
      var listLines = TextNoteArent.StorageLines.SelectMany(x => x.Value).ToList();
      document.Delete(listLines);

      TextNoteArent.StorageLines = new Dictionary<ElementId, List<ElementId>>();

      var allText = new FilteredElementCollector(document, document.ActiveView.Id).WhereElementIsNotElementType().OfClass(typeof(TextNote)).Cast<TextNote>().ToList();
      allText.ForEach(TextNoteArent.CreateSingleBoxText);
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

    public bool Register(Document document)
    {
      var viewFilter = new ElementClassFilter(typeof(View));
      var viewScale = new FilteredElementCollector(document).WhereElementIsNotElementType().OfClass(typeof(View)).First()
        .get_Parameter(BuiltInParameter.VIEW_SCALE);
      
      UpdaterRegistry.RegisterUpdater( this ) ;
      UpdaterRegistry.AddTrigger( GetUpdaterId(), viewFilter, Element.GetChangeTypeParameter(viewScale) ) ;
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