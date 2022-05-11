using System.Collections.Generic ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Helpers
{
  public static class PatternElementHelper
  {
    public static readonly List<string> PatternNames = new List<string>()
    {
      "天井隠蔽配管", // 00
      "床隠蔽配管", // 01
      "露出配管", // 02
      "天井内ケーブル配線", // 03
      "ケーブルラック配線", // 04
      "ケーブル配線（配管レス）", // 05
      "冷媒配管共巻", // 06
      "フリーアクセス内配線", // 07
      "ケーブルピット内配線", //08
      "地中埋設配管" //09
    } ;
    public static List<(string PatternName, ElementId PatternId)> GetLinePatterns( Document document )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Create Line Pattern" ) ;
      
      // 00
      var linePatterns = new List<(string PatternName, ElementId PatternId)> { new(PatternNames[0], LinePatternElement.GetSolidPatternId()) } ;

      // 01
      var patternElementTwo = LinePatternElement.GetLinePatternElementByName( document, PatternNames[1] ) ;
      if ( null == patternElementTwo ) {
        var linePattern = new LinePattern( PatternNames[1] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 10d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits())
        } ) ;
        patternElementTwo = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementTwo.GetLinePattern().Name, patternElementTwo.Id ) ) ;
      
      // 02
      var patternElementThree = LinePatternElement.GetLinePatternElementByName( document, PatternNames[2] ) ;
      if ( null == patternElementThree ) {
        var linePattern = new LinePattern( PatternNames[2] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 4d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 4d.MillimetersToRevitUnits())
        } ) ;
        patternElementThree = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementThree.GetLinePattern().Name, patternElementThree.Id ) ) ;
      
      // 03
      linePatterns.Add( ( PatternNames[3], LinePatternElement.GetSolidPatternId() ) ) ;
      
      // 04
      var patternElementFive = LinePatternElement.GetLinePatternElementByName( document, PatternNames[4] ) ;
      if ( null == patternElementFive ) {
        var linePattern = new LinePattern( PatternNames[4] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 6d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits())
        } ) ;
        patternElementFive = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementFive.GetLinePattern().Name, patternElementFive.Id ) ) ;
      
      // 05
      linePatterns.Add( ( PatternNames[5], LinePatternElement.GetSolidPatternId() ) ) ;
      
      // 06
      var patternElementSeven = LinePatternElement.GetLinePatternElementByName( document, PatternNames[6] ) ;
      if ( null == patternElementSeven ) {
        var linePattern = new LinePattern( PatternNames[6] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 6d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits())
        } ) ;
        patternElementSeven = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementSeven.GetLinePattern().Name, patternElementSeven.Id ) ) ;
      
      // 07
      var patternElementEight = LinePatternElement.GetLinePatternElementByName( document, PatternNames[7] ) ;
      if ( null == patternElementEight ) {
        var linePattern = new LinePattern( PatternNames[7] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 6d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits())
        } ) ;
        patternElementEight = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementEight.GetLinePattern().Name, patternElementEight.Id ) ) ;
      
      // 08
      var patternElementNine = LinePatternElement.GetLinePatternElementByName( document, PatternNames[8] ) ;
      if ( null == patternElementNine ) {
        var linePattern = new LinePattern( PatternNames[8] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 6d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits())
        } ) ;
        patternElementNine = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementNine.GetLinePattern().Name, patternElementNine.Id ) ) ;
      
      // 09
      var patternElementTen = LinePatternElement.GetLinePatternElementByName( document, PatternNames[9] ) ;
      if ( null == patternElementTen ) {
        var linePattern = new LinePattern( PatternNames[9] ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 6d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits()),
          new(LinePatternSegmentType.Dot, 0d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits())
        } ) ;
        patternElementTen = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementTen.GetLinePattern().Name, patternElementTen.Id ) ) ;

      transaction.Commit() ;
      
      return linePatterns ;
    }
  }
}