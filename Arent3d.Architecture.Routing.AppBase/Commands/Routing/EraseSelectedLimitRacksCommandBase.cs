using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseSelectedLimitRacksCommandBase :IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      
      var limitRackStorable = document.GetAllStorables<LimitRackStorable>().FirstOrDefault() ?? document.GetLimitRackStorable();

      var selectedLimitRacks = uiDocument.Selection
        .PickElementsByRectangle( CableTraySelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
        .Where( x => x is FamilyInstance  or CableTray) ;

      if ( selectedLimitRacks.Any() ) {
        
      }
      
      return Result.Succeeded ;
    }
  }
}