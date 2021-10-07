using Arent3d.Architecture.Routing.AppBase.Commands.Shaft;
using Arent3d.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using ImageType = Arent3d.Revit.UI.ImageType;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Shaft
{
    [Transaction(TransactionMode.Manual)]
    [DisplayNameKey("Electrical.App.Commands.Shaft.CreateArentShaftCommand", DefaultString = "Create\nArent Shaft")]
    [Image("resources/shaft_02.png")]
    public class CreateArentShaftCommand : CreateArentShaftCommandBase
    {
    }
}
