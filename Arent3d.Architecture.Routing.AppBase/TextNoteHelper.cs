using System.Linq ;
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
    
    public static TextNoteType? FindOrCreateTextNoteType(Document document)
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

      return textNoteType ;
    }
  }
}