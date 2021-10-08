using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing;
using Arent3d.Revit.UI;
using Autodesk.Revit.Attributes;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction(TransactionMode.Manual)]
  [DisplayNameKey("Electrical.App.Commands.Routing.ShowHeightSetting", DefaultString = "Height Setting")]
  [Image("resources/height_setting.png")]
  class ShowHeightSettingCommand : ShowHeightSettingCommandBase
  {
  }
}
