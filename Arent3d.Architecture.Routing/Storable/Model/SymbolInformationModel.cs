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
    public int Color { get ; set ; } = 3 ;
    public string Description { get ; set ; } = string.Empty ;
    public double CharacterHeight { get ; set ; } = 1 ; 
    public bool Visible { get ; set ; } = false ;
    public bool IsEco { get ; set ; } = false ;

    public SymbolInformationModel()
    {
      
    }
      
    public SymbolInformationModel( string? id,  string? symbolKind, string? symbolCoordinate, double? height, double? percent, int? color, bool? visible, string? description, double? characterHeight , bool? isEco)
    {
      Id = id ?? "-1" ;
      SymbolKind = symbolKind ?? SymbolKindEnum.Start.ToString() ;
      SymbolCoordinate = symbolCoordinate ?? SymbolCoordinateEnum.Right.ToString() ;
      Height = height ?? 3  ;
      Percent = percent ?? 90 ;
      Color = color ?? 3;
      Visible = visible ?? false ;
      Description = description ?? string.Empty ;
      CharacterHeight = characterHeight ?? 1 ;
      IsEco = isEco ?? false ;
    } 
  }
}