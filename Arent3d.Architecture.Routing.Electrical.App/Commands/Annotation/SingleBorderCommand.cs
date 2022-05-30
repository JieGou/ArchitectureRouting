﻿using System ;
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
  [DisplayNameKey( "Electrical.App.Commands.Annotation.SingleBorderCommand", DefaultString = "Simple Border\nText Box" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class SingleBorderCommand : IExternalCommand
  {
    public const string TextNoteTypeName = "ARENT_2.5MM_SIMPLE-BORDER" ;
    private const double TextSize = 2.5 ;
    private const double OffsetSheet = 0.6 ;
    private const int BackGround = 1 ;
    private const int VisibleBox = 0 ;
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var application = commandData.Application ;
      var document = application.ActiveUIDocument.Document ;

      using var transaction = new Transaction( document ) ;
      transaction.Start( "Simple TextNote Border" ) ;

      var textNoteType = FindOrCreateTextNoteType( document, TextNoteTypeName ) ;
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

    public static TextNoteType? FindOrCreateTextNoteType( Document document, string textNoteTypeName )
    {
      var textNoteTypes = new FilteredElementCollector( document ).OfClass( typeof( TextNoteType ) ).OfType<TextNoteType>().EnumerateAll() ;
      if ( ! textNoteTypes.Any() )
        return null ;

      var textNoteType = textNoteTypes.SingleOrDefault( x => x.Name == textNoteTypeName ) ;
      if ( null != textNoteType ) {
        if ( textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ) is { } textSize && Math.Abs( textSize.AsDouble().RevitUnitsToMillimeters() - TextSize ) > GeometryHelper.Tolerance )
          textSize.Set( TextSize.MillimetersToRevitUnits() ) ;
        
        if ( textNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ) is { } leaderOffsetSheet && Math.Abs( leaderOffsetSheet.AsDouble().RevitUnitsToMillimeters() - OffsetSheet ) > GeometryHelper.Tolerance )
          leaderOffsetSheet.Set( OffsetSheet.MillimetersToRevitUnits() ) ;
        
        if ( textNoteType.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ) is { } backGround && backGround.AsInteger() != BackGround )
          backGround.Set( BackGround ) ;
        
        if ( textNoteType.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ) is { } visibleBox && visibleBox.AsInteger() != VisibleBox )
          visibleBox.Set( VisibleBox ) ;
        
        return textNoteType ;
      }

      textNoteType = textNoteTypes.First().Duplicate( textNoteTypeName ) as TextNoteType ;
      if ( null == textNoteType )
        return null ;

      textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( TextSize.MillimetersToRevitUnits() ) ;
      textNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).Set( OffsetSheet.MillimetersToRevitUnits() ) ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( BackGround ) ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( VisibleBox ) ;

      return textNoteType ;
    }
  }
}