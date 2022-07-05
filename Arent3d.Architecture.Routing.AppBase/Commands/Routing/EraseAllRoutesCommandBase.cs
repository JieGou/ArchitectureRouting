using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class EraseAllRoutesCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      var cache = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var hashSet = commandData.Application.ActiveUIDocument.Document.CollectRoutes( GetAddInType() ).Select( route => route.RouteName ).ToHashSet() ;
      if ( hashSet.Any() ) ChangeWireTypeCommand.RemoveDetailLinesByRoutes( document, hashSet ) ;

      try {
        return document.Transaction( "TransactionName.Commands.Routing.EraseAllRoutes".GetAppStringByKeyOrDefault( null ), _ =>
        {
          RouteGenerator.EraseRoutes( document, hashSet, true, true ) ;
          cache.Drop( hashSet ) ;
          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    protected abstract AddInType GetAddInType() ;
  }
}