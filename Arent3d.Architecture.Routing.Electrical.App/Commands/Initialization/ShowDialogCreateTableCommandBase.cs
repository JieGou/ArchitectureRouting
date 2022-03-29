using Arent3d.Architecture.Routing.AppBase.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
    public class ShowDialogCreateTableCommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var document = commandData.Application.ActiveUIDocument.Document;

            var dialog = new CreateTableByFloors(document);
            dialog.ShowDialog();
            return Result.Succeeded;
        }
    }
}
