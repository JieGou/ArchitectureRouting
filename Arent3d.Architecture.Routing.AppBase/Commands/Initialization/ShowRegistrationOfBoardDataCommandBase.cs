using Arent3d.Architecture.Routing.AppBase.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
    public class ShowRegistrationOfBoardDataCommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document ;

            var dlgRegistrationOfBoardDataModel = new RegistrationOfBoardDataDialog( commandData.Application ) ;

            dlgRegistrationOfBoardDataModel.ShowDialog() ;
            if ( ! ( dlgRegistrationOfBoardDataModel.DialogResult ?? false ) ) return Result.Cancelled ;
            return Result.Succeeded;
        }
    }
}