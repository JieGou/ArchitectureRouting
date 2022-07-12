using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class PickUpNumberSettingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var viewModel = new PickUpNumberSettingViewModel( document ) ;
      var dialog = new PickUpNumberSettingDialog( viewModel ) ;

      dialog.ShowDialog() ;
      if ( dialog.DialogResult == false ) return Result.Cancelled ;

      return Result.Succeeded ;
    }
  }
}