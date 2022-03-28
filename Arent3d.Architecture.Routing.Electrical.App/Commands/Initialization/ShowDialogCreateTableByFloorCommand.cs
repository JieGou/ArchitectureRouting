using Arent3d.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using ImageType = Arent3d.Revit.UI.ImageType;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
    [Transaction(TransactionMode.Manual)]
    [DisplayNameKey("Electrical.App.Commands.Routing.ShowCreateTableByFloorDialogCommand", DefaultString = "Create table by floors")]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    internal class ShowDialogCreateTableByFloorCommand : ShowDialogCreateTableCommandBase
    {
    }
}
