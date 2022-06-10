namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class ElectricalCategoryModel
  {
    public string Col1 { get ; set ; }
    public string Col2 { get ; set ; }
    public string Col3 { get ; set ; }

    public ElectricalCategoryModel( string? col1, string? col2, string? col3 )
    {
      Col1 = col1 ?? string.Empty ;
      Col2 = col2 ?? string.Empty ;
      Col3 = col3 ?? string.Empty ;
    }
  }
}