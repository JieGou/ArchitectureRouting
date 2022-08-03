using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class PickUpReportCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var pickUpReportViewModel = new PickUpReportViewModel( document ) ;
      var dialog = new PickUpReportDialog( pickUpReportViewModel ) ;

      dialog.ShowDialog() ;
      if ( dialog.DialogResult ?? false ) {
        return Result.Succeeded ;
      }
      else {
        return Result.Cancelled ;
      }
    }
  }
}