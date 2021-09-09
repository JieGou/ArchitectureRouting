using Arent3d.Architecture.Routing.AppBase.Commands.Shaft;
using Arent3d.Architecture.Routing.AppBase;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.EndPoints;
using Arent3d.Revit;
using Arent3d.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using ImageType = Arent3d.Revit.UI.ImageType;
namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Shaft
{
    [Transaction(TransactionMode.Manual)]
    [DisplayNameKey("Electrical.App.Commands.Shaft.CreateShaftCommand", DefaultString = "Create\nShaft")]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    public class CreateShaftCommand : CreateShaftCommandBase
    {
    }
}
