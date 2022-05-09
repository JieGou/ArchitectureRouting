using System.Collections.Generic ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Helpers
{
  public static class PatternElementHelper
  {
    public static List<(string Name, ElementId PatternId)> GetLinePatterns( Document document )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Create Line Pattern" ) ;
      
      // 天井隠蔽配管
      var linePatterns = new List<(string Name, ElementId PatternId)> { new("天井隠蔽配管", LinePatternElement.GetSolidPatternId()) } ;

      // 床隠蔽配管
      var patternElementTwo = LinePatternElement.GetLinePatternElementByName( document, "床隠蔽配管" ) ;
      if ( null == patternElementTwo ) {
        var linePattern = new LinePattern( "床隠蔽配管" ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 10d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 2d.MillimetersToRevitUnits())
        } ) ;
        patternElementTwo = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementTwo.GetLinePattern().Name, patternElementTwo.Id ) ) ;
      
      // 露出配管
      var patternElementThree = LinePatternElement.GetLinePatternElementByName( document, "露出配管" ) ;
      if ( null == patternElementThree ) {
        var linePattern = new LinePattern( "露出配管" ) ;
        linePattern.SetSegments( new List<LinePatternSegment>
        {
          new(LinePatternSegmentType.Dash, 4d.MillimetersToRevitUnits()), 
          new(LinePatternSegmentType.Space, 4d.MillimetersToRevitUnits())
        } ) ;
        patternElementThree = LinePatternElement.Create( document, linePattern ) ;
      }
      linePatterns.Add( ( patternElementThree.GetLinePattern().Name, patternElementThree.Id ) ) ;
      
      // 天井内ケーブル配線
      linePatterns.Add( ( "天井内ケーブル配線", LinePatternElement.GetSolidPatternId() ) ) ;
      
      // ケーブルラック配線
      var patternElementFive = LinePatternElement.GetLinePatternElementByName( document, "ケーブルラック配線" ) ;
      if ( null == patternElementFive ) {
        var linePattern = new LinePattern( "ケーブルラック配線" ) ;
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
      
      // ケーブル配線（配管レス
      linePatterns.Add( ( "ケーブル配線（配管レス", LinePatternElement.GetSolidPatternId() ) ) ;
      
      // 冷媒配管共巻
      var patternElementSeven = LinePatternElement.GetLinePatternElementByName( document, "冷媒配管共巻" ) ;
      if ( null == patternElementSeven ) {
        var linePattern = new LinePattern( "冷媒配管共巻" ) ;
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
      
      // フリーアクセス内配線
      var patternElementEight = LinePatternElement.GetLinePatternElementByName( document, "フリーアクセス内配線" ) ;
      if ( null == patternElementEight ) {
        var linePattern = new LinePattern( "フリーアクセス内配線" ) ;
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
      
      // ケーブルピット内配線
      var patternElementNine = LinePatternElement.GetLinePatternElementByName( document, "ケーブルピット内配線" ) ;
      if ( null == patternElementNine ) {
        var linePattern = new LinePattern( "ケーブルピット内配線" ) ;
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
      
      // 地中埋設配管
      var patternElementTen = LinePatternElement.GetLinePatternElementByName( document, "地中埋設配管" ) ;
      if ( null == patternElementTen ) {
        var linePattern = new LinePattern( "地中埋設配管" ) ;
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