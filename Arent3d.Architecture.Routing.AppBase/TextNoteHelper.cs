using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public static class TextNoteHelper
  {
    public const string RextNoteTypeName = "ARENT_2.7MM_0.75" ;
    public static double TextSize => 2.7 ;
    public static double LeaderOffsetSheet => 0.6;
    public static double TotalHeight => TextSize + 2 * LeaderOffsetSheet ;
    
    public static TextNoteType? FindOrCreateTextNoteType(Document document, bool isVisible = true)
    {
      var textNoteTypes = new FilteredElementCollector( document ).OfClass( typeof( TextNoteType ) ).OfType<TextNoteType>().EnumerateAll() ;
      if ( ! textNoteTypes.Any() )
        return null ;
      
      var textNoteType = textNoteTypes.SingleOrDefault( x => x.Name == RextNoteTypeName ) ;
      if ( null != textNoteType ) 
        return textNoteType ;
      
      textNoteType = textNoteTypes.First().Duplicate(RextNoteTypeName) as TextNoteType;
      if ( null == textNoteType )
        return null ;
      
      textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( TextSize.MillimetersToRevitUnits() ) ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_WIDTH_SCALE ).Set( 0.75 ) ;
      textNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).Set( LeaderOffsetSheet.MillimetersToRevitUnits() ) ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( 1 ) ;

      if( !isVisible )
        textNoteType.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;
      
      return textNoteType ;
    }

    public static TextNoteType? FindOrCreateTextNoteType(Document document, double textSize, bool isVisible = true)
    {
      var textNoteTypeName = $"ARENT3D_{Math.Round( textSize, 2 )}MM_0.75" ;
      var textNoteTypes = new FilteredElementCollector( document ).OfClass( typeof( TextNoteType ) ).OfType<TextNoteType>().EnumerateAll() ;
      if ( ! textNoteTypes.Any() )
        return null ;
      
      var textNoteType = textNoteTypes.SingleOrDefault( x => x.Name == textNoteTypeName ) ;
      if ( null != textNoteType ) 
        return textNoteType ;
      
      textNoteType = textNoteTypes.First().Duplicate(textNoteTypeName) as TextNoteType;
      if ( null == textNoteType )
        return null ;
      
      textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( textSize.MillimetersToRevitUnits() ) ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_WIDTH_SCALE ).Set( 0.75 ) ;
      textNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).Set( LeaderOffsetSheet.MillimetersToRevitUnits() ) ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( 1 ) ;
      
      if( !isVisible )
        textNoteType.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;

      return textNoteType ;
    }

    public static void DeleteAllTextNotesRelatedStorages( Document document )
    {
      // Pull boxes
      var textNotesOfPullBoxIds = document.GetAllDatas<Level, PullBoxInfoModel>().SelectMany( d => d.Data.PullBoxInfoData ).Select( d => document.GetElement( d.TextNoteUniqueId ) ).Where( e => e != null ).Select( t => t.Id ).ToList() ;
      document.Delete( textNotesOfPullBoxIds ) ;
      
      // Wire length notation
      var wireLengthNotationIds = document.GetWireLengthNotationStorable().WireLengthNotationData.Select( d => document.GetElement( d.TextNoteId ) ).Where( e => e != null ).Select( t => t.Id ).ToList() ;
      document.Delete( wireLengthNotationIds ) ;
    }
  }
}