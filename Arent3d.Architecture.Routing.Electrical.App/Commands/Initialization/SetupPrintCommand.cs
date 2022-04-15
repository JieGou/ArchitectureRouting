using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.SetupPrintCommand", DefaultString = "Setup Print" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class SetupPrintCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var externalEventHandler = new ExternalEventHandler() ;
      var dataContext = new SetupPrintViewModel( commandData.Application.ActiveUIDocument ) { ExternalEventHandler = externalEventHandler } ;
      var setupPrintView = new SetupPrintView { DataContext = dataContext } ;
      externalEventHandler.ExternalEvent = ExternalEvent.Create( dataContext.ExternalEventHandler ) ;
      setupPrintView.Show() ;
      return Result.Succeeded ;
    }
  }
}