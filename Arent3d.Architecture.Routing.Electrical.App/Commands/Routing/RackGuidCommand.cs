using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
    [Transaction( TransactionMode.Manual )]
    [DisplayNameKey( "Electrical.App.Commands.Routing.RackGuidCommand", DefaultString = "Rack Guid\nPS" )]
    [Image( "resources/PickFrom-To.png" )]
    public class RackGuidCommand : IExternalCommand
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