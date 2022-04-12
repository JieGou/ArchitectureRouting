using System;
using System.Collections.Generic;
using System.Linq;
using Arent3d.Revit;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Updater
{
public class ViewUpdater : IUpdater
  {
    private static UpdaterId? _updaterId ;
    private readonly bool _isDoubleBorder ;
    public ViewUpdater( AddInId? id, bool isDoubleBorder )
    {
      _isDoubleBorder = isDoubleBorder ;
      _updaterId = new UpdaterId( id, Guid.NewGuid() ) ;
    }

    public void Execute( UpdaterData data )
    {
      var document = data.GetDocument();
      if(document is null) return;
      var listLines = ArentTextNote.StorageLines.Where(x=>ArentTextNote.CheckIdIsDeleted(document, x.Key))
        .SelectMany(x => x.Value).ToList();
      document.Delete(listLines.Where(x=>ArentTextNote.CheckIdIsDeleted(document, x)).ToList());

      ArentTextNote.StorageLines = new Dictionary<ElementId, List<ElementId>>();
      var allText = new FilteredElementCollector(document, document.ActiveView.Id).WhereElementIsNotElementType().OfClass(typeof(TextNote)).Cast<TextNote>();
      var filterText = allText.Where(t => t.TextNoteType.Name == ArentTextNote.ArentTextNoteType).ToList();
      if (_isDoubleBorder)
        filterText.ForEach(ArentTextNote.CreateDoubleBorderText);
      else
        filterText.ForEach(ArentTextNote.CreateSingleBorderText);
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
      return "ViewUpdater" ;
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