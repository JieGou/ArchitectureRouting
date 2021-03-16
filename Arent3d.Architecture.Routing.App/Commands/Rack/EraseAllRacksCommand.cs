using System ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Rack
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Delete\nAll PS" )]
  [Image( "resources/DeleteAllPS.png" )]
  public class EraseAllRacksCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      using var tx = new Transaction( document ) ;
      tx.Start( "Erase all routes" ) ;
      try {
        document.Delete( document.GetAllFamilyInstances( RoutingFamilyType.RackGuide ).Select( fi => fi.Id ).ToList() ) ;

        tx.Commit() ;
      }
      catch {
        tx.RollBack() ;
        return Result.Failed ;
      }

      return Result.Succeeded ;
    }
  }
}