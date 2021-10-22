using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.OffsetSettingCommand", DefaultString = "Offset\nSetting" )]
  [Image( "resources/PickFrom-To.png" )]
  public class OffsetSettingCommand : OffsetSettingCommandBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
  }
}