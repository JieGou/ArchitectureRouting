using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Updater ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Annotation.DoubleBorderCommand", DefaultString = "Double Border" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class DoubleBorderCommand : IExternalCommand
  {
    public static bool IsClicked { get ; set ; }
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      IsClicked = true ;
      
      var application = commandData.Application ;
      var document = application.ActiveUIDocument.Document ;

      using var transaction = new Transaction( document ) ;
      transaction.Start( "Double TextNote Border" ) ;
      
      var textNoteType = FindOrCreateTextNoteType( document ) ;
      if ( null == textNoteType ) {
        message = "Cannot create text note type!" ;
        return Result.Failed ;
      }

      if(document.IsDefaultElementTypeIdValid(ElementTypeGroup.TextNoteType, textNoteType.Id))
        document.SetDefaultElementTypeId(ElementTypeGroup.TextNoteType, textNoteType.Id);

      transaction.Commit() ;
      
      var textCommandId = RevitCommandId.LookupPostableCommandId(PostableCommand.Text);
      if(application.CanPostCommand(textCommandId))
        application.PostCommand(textCommandId);

      return Result.Succeeded ;
    }

    private static TextNoteType? FindOrCreateTextNoteType(Document document)
    {
      var textNoteTypes = new FilteredElementCollector( document ).OfClass( typeof( TextNoteType ) ).OfType<TextNoteType>().EnumerateAll() ;
      if ( ! textNoteTypes.Any() )
        return null ;
      
      var textNoteType = textNoteTypes.SingleOrDefault( x => x.Name == SimpleBorderCommand.TextNoteTypeName ) ;
      if ( null != textNoteType ) 
        return textNoteType ;
      
      textNoteType = textNoteTypes.First().Duplicate(SimpleBorderCommand.TextNoteTypeName) as TextNoteType;
      if ( null == textNoteType )
        return null ;
      
      textNoteType.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 1 ) ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( 2.5.MillimetersToRevitUnits() ) ;

      return textNoteType ;
    }
    
    public static void RegisterUpdater(DocumentChangedEventArgs e)
    {
      try {
        if ( ! IsClicked ) return ;
        IsClicked = false ;

        if ( e.GetAddedElementIds().Count != 1 )
          return ;

        var document = e.GetDocument() ;
        if ( document.GetElement( e.GetAddedElementIds().First() ) is not TextNote textNote )
          return ;

        if ( document.GetElement( textNote.GetTypeId() ) is not TextNoteType textNoteType )
          return ;

        //If config follows suggest of Rider will error when built
        if ( textNoteType.Name != SimpleBorderCommand.TextNoteTypeName )
          return ;

        using var transaction = new Transaction( document ) ;
        transaction.Start( "Create Double Border" ) ;

        var textNoteUpdater = new TextNoteUpdater( document.Application.ActiveAddInId ) ;
        if ( ! UpdaterRegistry.IsUpdaterRegistered( textNoteUpdater.GetUpdaterId() ) )
          UpdaterRegistry.RegisterUpdater( textNoteUpdater, document ) ;

        var curveLoop = GeometryHelper.GetOutlineTextNote( textNote ) ;
        var borderUniqueIds = ( from curve in curveLoop select document.Create.NewDetailCurve( document.ActiveView, curve ) into detailLine select detailLine.UniqueId ).ToList() ;

        var borderTextNoteStorable = document.GetAllStorables<BorderTextNoteStorable>().FirstOrDefault() ?? document.GetBorderTextNoteStorable() ;
        borderTextNoteStorable.BorderTextNoteData.Add( new BorderTextNoteModel( textNote.UniqueId, string.Join( ",", borderUniqueIds ) ) ) ;
        borderTextNoteStorable.Save() ;

        UpdaterRegistry.AddTrigger( textNoteUpdater.GetUpdaterId(), document, e.GetAddedElementIds(), Element.GetChangeTypeAny() ) ;

        transaction.Commit() ;
      }
      catch ( Exception exception ) {
        var str = exception.Message ;
      }
    }
  }
}