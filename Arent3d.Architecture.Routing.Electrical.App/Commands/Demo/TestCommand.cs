using System ;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Demo
{
    [Transaction( TransactionMode.Manual )]
    [DisplayNameKey( "Electrical.App.Commands.Demo.TestCommand", DefaultString = "Test" )]
    [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
    public class TestCommand : IExternalCommand
    {
        public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
        {
            var uiDocument = commandData.Application.ActiveUIDocument ;
            TabPlaceExternal? tabPlaceExternal = null ;
            try {
                
            }
            catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
                // Ignore
            }
            catch ( Exception exception ) {
                TaskDialog.Show( "PECC2", exception.Message ) ;
            }
            finally {
                using var trans = new Transaction(uiDocument.Document);
                trans.Start("Create");

                // var radius = UnitUtils.ConvertToInternalUnits(200, UnitTypeId.Millimeters);
                // uiDocument.Document.Create.NewDetailCurve(uiDocument.ActiveView, Arc.Create(tabPlaceExternal?.DrawingServer.NextPoint, radius, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY));

                trans.Commit();
                tabPlaceExternal?.Dispose();
            }
            return Result.Succeeded ;
        }
    }
}