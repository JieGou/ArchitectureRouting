using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.CommandTermCaches ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Delete All From-To" )]
  [Image( "resources/DeleteAllFrom-To.png" )]
  public class EraseAllRoutesCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var cache = RouteCache.Get( document ) ;
      var hashSet = cache.Keys.ToHashSet() ;

      using var tx = new Transaction( document ) ;
      tx.Start( "Erase all routes" ) ;
      try {
        RouteGenerator.EraseRoutes( document, hashSet, true ) ;
        cache.Drop( hashSet ) ;

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