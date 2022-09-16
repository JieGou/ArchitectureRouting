using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.DefaultSettingCommand", DefaultString = "Default Setting" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class DefaultSettingCommand : DefaultSettingCommandBase
  {
    protected override void UpdateCeedDockPaneDataContext( UIDocument uiDocument )
    {
      RoutingAppUI.CeedModelDockPanelProvider?.CustomInitiator( uiDocument, uiDocument.Document ) ;
    }
  }
}