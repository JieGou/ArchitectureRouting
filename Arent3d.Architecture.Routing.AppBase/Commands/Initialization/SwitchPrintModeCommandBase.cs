using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class SwitchPrintModeCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      Document document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction( "TransactionName.Commands.Routing.SwitchPrintMode".GetAppStringByKeyOrDefault( "Switch Print Mode" ), _ =>
        {
          var textNotesOfPullBoxes = PullBoxRouteManager.GetTextNotesOfPullBox( document ).EnumerateAll() ;

          if ( !textNotesOfPullBoxes.All( t => t.IsHidden( document.ActiveView ) ) )
            document.ActiveView.HideElements( textNotesOfPullBoxes.Select( t => t.Id ).ToList() );
          else 
            document.ActiveView.UnhideElements( textNotesOfPullBoxes.Select( t => t.Id ).ToList() );

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