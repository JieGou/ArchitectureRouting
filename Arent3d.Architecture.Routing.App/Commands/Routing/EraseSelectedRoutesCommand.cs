using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.Exceptions ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Erase Selected Routes" )]
  [Image( "resources/MEP.ico" )]
  public class EraseSelectedRoutesCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        var document = uiDocument.Document ;
        var selectedRoutes = Route.CollectAllDescendantBranches( SelectRoutes( uiDocument ) ) ;

        using var tx = new Transaction( document ) ;
        tx.Start( "Erase selected routes" ) ;
        try {
          RouteGenerator.EraseRoutes( document, selectedRoutes.Select( route => route.RouteName ), true ) ;

          tx.Commit() ;
        }
        catch {
          tx.RollBack() ;
          return Result.Failed ;
        }

        return Result.Succeeded ;
      }
      catch ( OperationCanceledException ) {
        return Result.Cancelled ;
      }
    }

    private IReadOnlyCollection<Route> SelectRoutes( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
      if ( 0 < list.Count ) return list ;

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to delete." ) ;
      return new[] { pickInfo.Route } ;
    }
  }
}