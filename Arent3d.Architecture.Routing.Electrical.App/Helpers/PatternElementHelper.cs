using System.Collections.Generic ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Helpers
{
  public static class PatternElementHelper
  {
    public static readonly Dictionary<int, string> PatternNames = new()
    {
      { 01, "天井隠蔽配管" },
      { 02, "床隠蔽配管" },
      { 03, "露出配管" },
      { 04, "天井内ケーブル配線" },
      { 05, "ケーブルラック配線" },
      { 06, "ケーブル配線（配管レス）" },
      { 07, "冷媒配管共巻" },
      { 08, "フリーアクセス内配線" },
      { 09, "ケーブルピット内配線" },
      { 10, "地中埋設配管" }
    } ;

    public static List<(string PatternName, ElementId PatternId)> GetLinePatterns( Document document )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Create Line Pattern" ) ;

      // 01
      var linePatterns = new List<(string PatternName, ElementId PatternId)> { new(PatternNames[ 01 ], LinePatternElement.GetSolidPatternId()) } ;

      // 02
      var patternElementTwo = LinePatternElement.GetLinePatternElementByName( document, PatternNames[ 02 ] ) ;
      if ( null == patternElementTwo ) {
        var linePattern = new LinePattern( PatternNames[ 02 ] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 2d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits())
        } ) ;
        patternElementTwo = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementTwo.GetLinePattern().Name, patternElementTwo.Id ) ) ;

      // 03
      var patternElementThree = LinePatternElement.GetLinePatternElementByName( document, PatternNames[ 03 ] ) ;
      if ( null == patternElementThree ) {
        var linePattern = new LinePattern( PatternNames[ 03 ] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 2d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits())
        } ) ;
        patternElementThree = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementThree.GetLinePattern().Name, patternElementThree.Id ) ) ;

      // 04
      linePatterns.Add( ( PatternNames[ 04 ], LinePatternElement.GetSolidPatternId() ) ) ;

      // 05
      var patternElementFive = LinePatternElement.GetLinePatternElementByName( document, PatternNames[ 05 ] ) ;
      if ( null == patternElementFive ) {
        var linePattern = new LinePattern( PatternNames[ 05 ] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits())
        } ) ;
        patternElementFive = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementFive.GetLinePattern().Name, patternElementFive.Id ) ) ;

      // 06
      linePatterns.Add( ( PatternNames[ 06 ], LinePatternElement.GetSolidPatternId() ) ) ;

      // 07
      var patternElementSeven = LinePatternElement.GetLinePatternElementByName( document, PatternNames[ 07 ] ) ;
      if ( null == patternElementSeven ) {
        var linePattern = new LinePattern( PatternNames[ 07 ] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits())
        } ) ;
        patternElementSeven = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementSeven.GetLinePattern().Name, patternElementSeven.Id ) ) ;

      // 08
      var patternElementEight = LinePatternElement.GetLinePatternElementByName( document, PatternNames[ 08 ] ) ;
      if ( null == patternElementEight ) {
        var linePattern = new LinePattern( PatternNames[ 08 ] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits())
        } ) ;
        patternElementEight = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementEight.GetLinePattern().Name, patternElementEight.Id ) ) ;

      // 09
      var patternElementNine = LinePatternElement.GetLinePatternElementByName( document, PatternNames[ 09 ] ) ;
      if ( null == patternElementNine ) {
        var linePattern = new LinePattern( PatternNames[ 09 ] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits())
        } ) ;
        patternElementNine = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementNine.GetLinePattern().Name, patternElementNine.Id ) ) ;

      // 10
      var patternElementTen = LinePatternElement.GetLinePatternElementByName( document, PatternNames[ 10 ] ) ;
      if ( null == patternElementTen ) {
        var linePattern = new LinePattern( PatternNames[ 10 ] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Space, 1d.MillimetersToRevitUnits())
        } ) ;
        patternElementTen = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementTen.GetLinePattern().Name, patternElementTen.Id ) ) ;

      transaction.Commit() ;

      return linePatterns ;
    }
  }
}