using System ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  public class ChangeLocationTypeCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var viewModel = new ChangeLocationTypeViewModel( commandData.Application.ActiveUIDocument ) ;
        var view = new ChangeLocationTypeView { DataContext = viewModel } ;
        view.ShowDialog() ;
        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }
  }
}