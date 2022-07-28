using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Extensions ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class PickUpFigureCreationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      
      try {
        var result = document.Transaction( "TransactionName.Commands.Initialization.PickUpFigureCreation".GetAppStringByKeyOrDefault( "Pick Up Figure Creation" ), _ =>
        {
          var level = document.ActiveView.GenLevel ;
          var wireLengthNotationStorable = document.GetWireLengthNotationStorable() ;
          var isDisplay = wireLengthNotationStorable.WireLengthNotationData.Any(tp => tp.Level == level.Name ) ;

          if ( ! isDisplay ) {
            var pickUpViewModel = new PickUpViewModel( document ) ;
            if ( ! pickUpViewModel.DataPickUpModels.Any() ) return Result.Cancelled ;
            
            var pickUpModels = pickUpViewModel.DataPickUpModels.Where( p => p.Floor == level.Name ).ToList() ;
            if ( ! pickUpModels.Any() ) {
              MessageBox.Show( "Don't have pick up data on this view.", "Message Warning" ) ;
              return Result.Cancelled ;
            }
            
            WireLengthNotationManager.ShowWireLengthNotation( wireLengthNotationStorable, document, level, pickUpModels ) ;
          }
          else {
            WireLengthNotationManager.RemoveWireLengthNotation( document, level.Name ) ;
          }
        
          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
  }

  public enum WireLengthNotationAlignment
  {
    Oblique, Vertical, Horizontal
  }
}