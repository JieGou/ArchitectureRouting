using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.RackGuideCommand", DefaultString = "Rack Guide\nPS" )]
  [Image( "resources/PickFrom-To.png" )]
  public class RackGuideCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument;
      var document = uiDocument.Document;
      var messagePoint = "Please Select Point";
      var pickPoint = uiDocument.Selection.PickPoint( messagePoint );

      using ( Transaction tr = new Transaction( document ) ) {
        tr.Start( "Create Rack Guide" );
        document.AddRackGuide( pickPoint, null );
        tr.Commit();
      }

      return Result.Succeeded;
    }
  }
}