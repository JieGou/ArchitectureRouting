using System.ComponentModel ;
using Arent3d.Architecture.Routing.App.Forms;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
    [Transaction( TransactionMode.Manual )]
    [DisplayNameKey( "App.Commands.Routing.ReplaceFromToCommand", DefaultString = "Replace\nFrom-To" )]
    [Image( "resources/ReplaceFromTo.png")]
    public class ReplaceFromToCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            var dialog = new ShowDialog(this.ToString());
            dialog.Show();

            return Result.Succeeded;
        }
    }
}