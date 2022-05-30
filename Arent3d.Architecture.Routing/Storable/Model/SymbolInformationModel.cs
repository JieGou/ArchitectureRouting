using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public enum SymbolKindEnum
  {
    星,
  }

  public enum SymbolCoordinateEnum
  {
    上,
    左,
    右,
    下,
  }

  public class SymbolInformationModel
  {
    public string Id { get ; init ; } = "-1" ;
    public string SymbolKind { get ; set ; } = SymbolKindEnum.星.ToString() ;
    public string SymbolCoordinate { get ; set ; } = SymbolCoordinateEnum.右.ToString() ;
    public double Height { get ; set ; } = 3 ;
    public double Percent { get ; set ; } = 90 ;
    public string Color { get ; set ; } = "Green";
    public string Floor { get ; init ; } = string.Empty ;
    public string Description { get ; set ; } = string.Empty ;
    public double CharacterHeight { get ; set ; } = 1 ;
    public bool IsShowText { get ; set ; } = true ;
    public bool IsEco { get ; }

    public SymbolInformationModel()
    {
    }

    public SymbolInformationModel( string? id, string? symbolKind, string? symbolCoordinate, double? height, double? percent, string? color, bool? isShowText, string? description, double? characterHeight, bool? isEco, string? floor )
    {
      Id = id ?? "-1" ;
      SymbolKind = symbolKind ?? SymbolKindEnum.星.ToString() ;
      SymbolCoordinate = symbolCoordinate ?? SymbolCoordinateEnum.右.ToString() ;
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
    public static readonly Dictionary<string, Color> DictSymbolColor = new()
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