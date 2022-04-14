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
  [DisplayNameKey( "Electrical.App.Commands.Initialization.RegisterSymbolCommand", DefaultString = "Register Symbol" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class RegisterSymbolCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      if ( uiDocument.ActiveView.ViewType != ViewType.FloorPlan )
        return Result.Cancelled ;
      
      var externalEventHandler = new ExternalEventHandler() ;
      var dataContext = new RegisterSymbolViewModel( uiDocument ) { ExternalEventHandler = externalEventHandler } ;
      externalEventHandler.ExternalEvent = ExternalEvent.Create( dataContext.ExternalEventHandler ) ;
      var registerSymbolView = new RegisterSymbolView { DataContext = dataContext } ;
      registerSymbolView.Show() ;

      return Result.Succeeded ;
    }
  }
}