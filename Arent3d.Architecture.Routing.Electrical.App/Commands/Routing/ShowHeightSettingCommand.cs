using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.ShowHeightSetting", DefaultString = "Height Setting" )]
  [Image( "resources/height_setting.png" )]
  public class ShowHeightSettingCommand : ShowHeightSettingCommandBase
  {
    protected override void AfterApplySetting()
    {
      UiDocument.Application.PostCommand<ReRouteAllAfterApplyHeightSettingCommand>() ;
    }
  }
}