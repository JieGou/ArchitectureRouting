namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class PickUpModel
  {
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
    public string Quantity { get ; set ; }
    public string Tani { get ; set ; }
    public string Supplement { get ; set ; }
    public string Supplement2 { get ; set ; }
    public string Glue { get ; set ; }
    public string Layer { get ; set ; }
    public string Classification { get ; set ; }

    public PickUpModel( string? item, string? floor, string? constructionItems, string? facility, string? productName, string? use, string? construction, string? modelNumber, string? specification, string? specification2, string? size, string? quantity, string? tani, string? supplement, string? supplement2, string? glue, string? layer, string? classification )
    {
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
      Quantity = quantity ?? string.Empty ;
      Tani = tani ?? string.Empty ;
      Supplement = supplement ?? string.Empty ;
      Supplement2 = supplement2 ?? string.Empty ;
      Glue = glue ?? string.Empty ;
      Layer = layer ?? string.Empty ;
      Classification = classification ?? string.Empty ;
    }
  }
}