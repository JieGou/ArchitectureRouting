namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class SymbolModel
  {
    public string SymbolKind { get ; set ; }
    public string SymbolCoordinate { get ; set ; }
    public double Height { get ; set ; }
    public double Percent { get ; set ; }
    public int Color { get ; set ; }
    public string Description { get ; set ; }
    public double CharacterHeight { get ; set ; }
    
    public SymbolModel( string? symbolKind, string? symbolCoordinate, double height, double percent, int color, string? description, double characterHeight )
    {
      SymbolKind = symbolKind ?? string.Empty ;
      SymbolCoordinate = symbolCoordinate ?? string.Empty ;
      Height = height ;
      Percent = percent ;
      Color = color ;
      Description = description ?? string.Empty ;
      CharacterHeight = characterHeight ;
    }
  }
}