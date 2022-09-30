using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.AppBase.Updater
{
  public class ViewUpdater : IUpdater
  {
    private readonly UpdaterId _updaterId ;

    public ViewUpdater( AddInId addInId )
    {
      _updaterId = new UpdaterId( addInId, Guid.Parse( "6B822580-DA20-4C8B-AD7C-656792C7E224" ) ) ;
    }

    public void Execute( UpdaterData updaterData )
    {
      var document = updaterData.GetDocument() ;
      var storageService = new StorageService<DataStorage, DisplaySettingModel>( document.FindOrCreateDataStorage<DisplaySettingModel>( false ) ) ;

      foreach ( var addedElementId in updaterData.GetAddedElementIds() ) {
        //TODO: Can't hide legend
        if ( !storageService.Data.IsLegendVisible && document.GetElement( addedElementId ) is Viewport viewport && document.GetElement( viewport.SheetId ) is ViewSheet viewSheetForLegend ) {
          viewSheetForLegend.HideElements(new List<ElementId> { viewport.Id });
        }
      
        if ( !storageService.Data.IsScheduleVisible && document.GetElement( addedElementId ) is ScheduleSheetInstance scheduleSheetInstance && document.GetElement( scheduleSheetInstance.OwnerViewId ) is ViewSheet viewSheetForSchedule ) {
          viewSheetForSchedule.HideElements(new List<ElementId> { scheduleSheetInstance.Id });
        }
      }
    }

    public UpdaterId GetUpdaterId()
    {
      return _updaterId ;
    }

    public ChangePriority GetChangePriority()
    {
      return ChangePriority.Views ;
    }

    public string GetUpdaterName()
    {
      return "Legend Arent Updater" ;
    }

    public string GetAdditionalInformation()
    {
      return "Arent, " + "https://arent3d.com" ;
    }
  }
}