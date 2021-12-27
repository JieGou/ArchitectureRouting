using Arent3d.Architecture.Routing.AppBase.Forms ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class PickUpReportCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var dialog = new PickUpReportDialog( document ) ;

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