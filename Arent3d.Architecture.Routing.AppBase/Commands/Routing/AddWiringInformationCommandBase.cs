using System ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AddWiringInformationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        Document document = uiDocument.Document ;

        var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to delete.", AddInType.Electrical ) ;
        //return new[] { pickInfo.Route } ;
      
        var viewModel = new AddWiringInformationViewModel( document, pickInfo.Route  ) ;
        var dialog = new AddWiringInformationDialog( viewModel ) ;

        dialog.ShowDialog() ;

        if ( dialog.DialogResult ?? false ) {
          return document.Transaction( "TransactionName.Commands.Routing.HeightSetting", _ =>
          {
            return Result.Succeeded ;
          } ) ;
        }

        return Result.Cancelled ; 
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ; 
      }
    } 
  }
}