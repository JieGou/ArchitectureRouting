using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class DisplaySettingByGradeCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var viewModel = new DisplaySettingByGradeViewModel( document ) ;
      var dialog = new DisplaySettingByGradeDialog( viewModel ) ;

      dialog.ShowDialog() ;
      return dialog.DialogResult == false ? Result.Cancelled : Result.Succeeded ;
    }
  }
}