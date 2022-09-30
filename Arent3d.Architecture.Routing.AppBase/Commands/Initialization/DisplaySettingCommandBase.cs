using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class DisplaySettingCommandBase : IExternalCommand
  {
    public const string LegendSelectionFilter = "ARENT_LEGEND" ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var viewModel = new DisplaySettingViewModel( document ) ;
      var dialog = new DisplaySettingDialog( viewModel ) ;

      dialog.ShowDialog() ;
      return dialog.DialogResult == false ? Result.Cancelled : Result.Succeeded ;
    }
  }
}