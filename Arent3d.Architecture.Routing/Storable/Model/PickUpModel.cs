namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class PickUpModel
  {
    public int Number { get ; set ; }
    public string Item { get ; set ; }
    public string Floor { get ; set ; }
    public string ConstructionItems { get ; set ; }
    public string Facility { get ; set ; }
    public string ProductName { get ; set ; }
    public string Use { get ; set ; }
    public string Construction { get ; set ; }
    public string ModelNumber { get ; set ; }
    public string Specification { get ; set ; }
    public string Specification2 { get ; set ; }
    public string Size { get ; set ; }
    public double Quantity { get ; set ; }
    public string Tani { get ; set ; }

    public PickUpModel( int? number, string? item, string? floor, string? constructionItems, string? facility, string? productName, string? use, string? construction, string? modelNumber, string? specification, string? specification2, string? size, double? quantity, string? tani )
    {
      Number = number ?? 0 ;
      Item = item ?? string.Empty ;
      Floor = floor ?? string.Empty ;
      ConstructionItems = constructionItems ?? string.Empty ;
      Facility = facility ?? string.Empty ;
      ProductName = productName ?? string.Empty ;
      Use = use ?? string.Empty ;
      Construction = construction ?? string.Empty ;
      ModelNumber = modelNumber ?? string.Empty ;
      Specification = specification ?? string.Empty ;
      Specification2 = specification2 ?? string.Empty ;
      Size = size ?? string.Empty ;
      Quantity = quantity ?? 0 ;
      Tani = tani ?? string.Empty ;
    }
  }
}