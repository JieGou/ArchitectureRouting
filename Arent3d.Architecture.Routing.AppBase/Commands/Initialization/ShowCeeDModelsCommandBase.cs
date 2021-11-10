using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowCeeDModelsCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      
      CeedStorable ceedStorable = document.GetCeeDStorable() ;

      var viewModel = new ViewModel.CeedViewModel( ceedStorable ) ;
      var dialog = new CeeDModelDialog( viewModel ) ;

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