using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class PickUpNumberSettingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var viewModel = new PickUpNumberSettingViewModel( document ) ;
      var dialog = new PickUpNumberSettingDialog( viewModel ) ;

      dialog.ShowDialog() ;
      return dialog.DialogResult == false
        ? Result.Cancelled
        : document.Transaction(
          "TransactionName.Commands.Initialization.PickUpNumberSetting".GetAppStringByKeyOrDefault(
            "Pick Up Number Setting" ), _ =>
          {
            var textNotePickUpStorable = document.GetTextNotePickUpStorable() ;
            var isDisplay = textNotePickUpStorable.TextNotePickUpData.Any() ;

            if ( !isDisplay ) return Result.Cancelled ;
            
            var pickUpViewModel = new PickUpViewModel( document ) ;
            var pickUpModels = pickUpViewModel.DataPickUpModels ;
            if ( !pickUpModels.Any() ) {
              MessageBox.Show( "Don't have pick up data.", "Message" ) ;
              return Result.Cancelled ;
            }
            PickUpMapCreationCommandBase.RemoveTextNotePickUp( document ) ;
            PickUpMapCreationCommandBase.ShowTextNotePickUp( textNotePickUpStorable, document, pickUpModels ) ;

            return Result.Succeeded ;
          } ) ;
    }
  }
}