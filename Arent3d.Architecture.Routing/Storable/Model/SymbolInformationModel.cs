using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public enum SymbolKindEnum
  {
    星,
    丸,
  }

  public enum SymbolCoordinate
  {
    上,
    左,
    中心,
    右,
    下,
  }

  public class SymbolInformationModel
  {
    public string SymbolUniqueId { get ; set ; } = string.Empty ;
    public string TagUniqueId { get ; set ; } = string.Empty ;
    public string SymbolKind { get ; set ; } = $"{SymbolKindEnum.星}" ;
    public string SymbolCoordinate { get ; set ; } = $"{Model.SymbolCoordinate.中心}" ;
    public double Height { get ; set ; } = 3 ;
    public double Percent { get ; set ; } = 100 ;
    public string Color { get ; set ; } = "Cyan";
    public string Floor { get ; set ; } = string.Empty ;
    public string Description { get ; set ; } = string.Empty ;
    public double CharacterHeight { get ; set ; } = 1 ;
    public bool IsShowText { get ; set ; } = true ;

    public SymbolInformationModel()
    {
      
    }

    public SymbolInformationModel( string? symbolUniqueId, string? tagUniqueId, string? symbolKind, string? symbolCoordinate, double? height, double? percent, string? color, bool? isShowText, string? description, double? characterHeight, string? floor )
    {
      SymbolUniqueId = symbolUniqueId ?? string.Empty ;
      TagUniqueId = tagUniqueId ?? string.Empty ;
      SymbolKind = symbolKind ?? $"{SymbolKindEnum.星}" ;
      SymbolCoordinate = symbolCoordinate ?? $"{Model.SymbolCoordinate.右}" ;
      Height = height ?? 3 ;
      Percent = percent ?? 90 ;
      Color = color ?? "Green" ;
      IsShowText = isShowText ?? false ;
      Description = description ?? string.Empty ;
      CharacterHeight = characterHeight ?? 1 ;
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