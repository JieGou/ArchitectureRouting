using System.ComponentModel ;
using System.Linq ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Erase All Routes" )]
  [Image( "resources/MEP.ico" )]
  public class DeleteAllRoutesCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var allRoutes = document.CollectRoutes().EnumerateAll() ;

      var endConnectors = allRoutes.SelectMany( route => route.GetAllConnectors( document ) ).EnumerateAll() ;
      var erasingRouteNames = allRoutes.Select( route => route.RouteId ).ToHashSet() ;

      using var tx = new Transaction( document ) ;
      tx.Start( "Setup routing" ) ;
      try {
        MEPSystemCreator.ErasePreviousRoutes( document, endConnectors, erasingRouteNames ) ;
        allRoutes.ForEach( route => route.Delete() ) ;
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