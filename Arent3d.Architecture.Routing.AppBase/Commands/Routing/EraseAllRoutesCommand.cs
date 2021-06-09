using System ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Routing.EraseAllRoutesCommand", DefaultString = "Delete\nAll From-To" )]
  [Image( "resources/DeleteAllFrom-To.png" )]
  public class EraseAllRoutesCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var cache = RouteCache.Get( document ) ;
      var hashSet = cache.Keys.ToHashSet() ;

      try {
        return document.Transaction( "TransactionName.Commands.Routing.EraseAllRoutes".GetAppStringByKeyOrDefault( null ), _ =>
        {
          RouteGenerator.EraseRoutes( document, hashSet, true ) ;
          cache.Drop( hashSet ) ;
          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
  }
}