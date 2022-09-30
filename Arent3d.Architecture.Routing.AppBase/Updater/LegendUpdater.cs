using System ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Updater
{
  public class LegendUpdater: IUpdater
  {
    private readonly UpdaterId _updaterId ;
    
    public LegendUpdater( AddInId addInId )
    {
      _updaterId = new UpdaterId( addInId, Guid.Parse("6B822580-DA20-4C8B-AD7C-656792C7E224") ) ;
    }
    
    public void Execute( UpdaterData updaterData )
    {
      DisplaySettingCommandBase.AddLegendToselectionFilter(updaterData.GetDocument(), updaterData.GetAddedElementIds());
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