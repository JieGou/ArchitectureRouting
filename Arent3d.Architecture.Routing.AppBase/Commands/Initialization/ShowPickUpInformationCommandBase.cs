using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowPickUpInformationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      PickUpViewModel pickUpViewModel = new PickUpViewModel( document ) ;
      var pickUpDialog = new PickupDialog( pickUpViewModel ) ;
      if(!pickUpViewModel.PickUpModels.Any())
        return Result.Cancelled ;
      
      pickUpDialog.ShowDialog() ;
      if ( pickUpDialog.DialogResult ?? false ) {
        return Result.Succeeded ;
      }
      else {
        return Result.Cancelled ;
      }
    }
  }
}