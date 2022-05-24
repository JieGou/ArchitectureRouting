using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public enum SymbolKindEnum
  {
    Start,
    Rectangle,
    Triangle,
  }

  public enum SymbolCoordinateEnum
  {
    Top,
    Left,
    Right,
    Bottom,
  }

  public class SymbolInformationModel
  {
    public string Id { get ; set ; } = "-1" ;
    public string SymbolKind { get ; set ; } = SymbolKindEnum.Start.ToString() ;
    public string SymbolCoordinate { get ; set ; } = SymbolCoordinateEnum.Right.ToString() ;
    public double Height { get ; set ; } = 3 ;
    public double Percent { get ; set ; } = 90 ;
    public string Color { get ; set ; } = "Green";
    public string Floor { get ; set ; } = string.Empty ;
    public string Description { get ; set ; } = string.Empty ;
    public double CharacterHeight { get ; set ; } = 1 ;
    public bool IsShowText { get ; set ; } = true ;
    public bool IsEco { get ; set ; } = false ;

    public SymbolInformationModel()
    {
    }

    public SymbolInformationModel( string? id, string? symbolKind, string? symbolCoordinate, double? height, double? percent, string? color, bool? isShowText, string? description, double? characterHeight, bool? isEco, string? floor )
    {
      Id = id ?? "-1" ;
      SymbolKind = symbolKind ?? SymbolKindEnum.Start.ToString() ;
      SymbolCoordinate = symbolCoordinate ?? SymbolCoordinateEnum.Right.ToString() ;
      Height = height ?? 3 ;
      Percent = percent ?? 90 ;
      Color = color ?? "Green" ;
      IsShowText = isShowText ?? false ;
      Description = description ?? string.Empty ;
      CharacterHeight = characterHeight ?? 1 ;
      IsEco = isEco ?? false ;
      Floor = floor ?? string.Empty ;
    }
  }

  public static class SymbolColor
  {
    // color like AutoCAD color Index: https://gohtx.com/acadcolors.php
    public static Dictionary<string, Color> DictSymbolColor = new()
    {
      { "Red", new Color( 255, 0, 0 ) },
      { "Yellow", new Color( 255, 255, 0 ) },
      { "Green", new Color( 0, 255, 0 ) },
      { "Cyan", new Color( 0, 255, 255 ) },
      { "Blue", new Color( 0, 0, 255 ) },
      { "Purple", new Color( 255, 0, 255 ) },
      { "White", new Color( 255, 255, 255 ) }
    } ;
  }
}