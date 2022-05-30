using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Updater ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Annotation.DoubleBorderCommand", DefaultString = "Double Border\nText Box" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class DoubleBorderCommand : IExternalCommand
  {
    public const string TextNoteTypeName = "ARENT_2.5MM_DOUBLE-BORDER" ;
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var application = commandData.Application ;
      var document = application.ActiveUIDocument.Document ;

      using var transaction = new Transaction( document ) ;
      transaction.Start( "Double TextNote Border" ) ;

      var textNoteType = SingleBorderCommand.FindOrCreateTextNoteType( document, TextNoteTypeName ) ;
      if ( null == textNoteType ) {
        message = "Cannot create text note type!" ;
        return Result.Failed ;
      }

      if ( document.IsDefaultElementTypeIdValid( ElementTypeGroup.TextNoteType, textNoteType.Id ) )
        document.SetDefaultElementTypeId( ElementTypeGroup.TextNoteType, textNoteType.Id ) ;

      var textNoteUpdater = new TextNoteUpdater( document.Application.ActiveAddInId ) ;
      if ( ! UpdaterRegistry.IsUpdaterRegistered( textNoteUpdater.GetUpdaterId() ) ) {
        UpdaterRegistry.RegisterUpdater( textNoteUpdater, document ) ;
        var filter = new ElementClassFilter( typeof( TextNote ) ) ;
        var changeType = ChangeType.ConcatenateChangeTypes( Element.GetChangeTypeElementAddition(), Element.GetChangeTypeElementDeletion() ) ;
        UpdaterRegistry.AddTrigger( textNoteUpdater.GetUpdaterId(), document, filter, ChangeType.ConcatenateChangeTypes( Element.GetChangeTypeAny(), changeType ) ) ;
      }

      transaction.Commit() ;

      var textCommandId = RevitCommandId.LookupPostableCommandId( PostableCommand.Text ) ;
      if ( application.CanPostCommand( textCommandId ) )
        application.PostCommand( textCommandId ) ;

      return Result.Succeeded ;
    }
  }
}