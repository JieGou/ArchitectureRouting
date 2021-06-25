using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
    public abstract class RackGuidCommandBase : IExternalCommand
    {
        // protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.RackGuid";


        public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
        {

            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;
            var messagePoint = "Please Select Point";
            var pickedObject = uiDocument.Selection.PickPoint( messagePoint );

            using ( Transaction tr = new Transaction( document ) ) {
                tr.Start( "Create Rack Guid" );
                document.AddRackGuid( pickedObject );
                tr.Commit();
            }

            return Result.Succeeded;
        }

  }
}