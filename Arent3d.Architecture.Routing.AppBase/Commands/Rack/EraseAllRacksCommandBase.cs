using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Rack
{
  public abstract class EraseAllRacksCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        var result = document.Transaction( "TransactionName.Commands.Rack.EraseAll".GetAppStringByKeyOrDefault( "Erase All Pipe Spaces" ), _ =>
        {
          document.Delete( document.GetAllFamilyInstances( RoutingFamilyType.RackGuide ).Select( fi => fi.Id ).ToList() ) ;
          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
  }
}