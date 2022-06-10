namespace Arent3d.Architecture.Routing.Storable.Model
{ 
  public class CeedDetailModel
  {
    public string ProductCode { get ; set ; }
    public string ProductName { get ; set ; }
    public string Standard { get ; set ; }
    public string Classification { get ; set ; }
    public string Size1 { get ; set ; }
    public string Size2 { get ; set ; }
    public double Quantity { get ; set ; }
    public string Unit { get ; set ; }
    public string ParentId { get ; set ; } 
    public string Trajectory { get ; set ; } 
    public string Specification { get ; set ; } 
    public int Order { get ; set ; }
    
    public string ModeNumber { get ; set ; }

    public CeedDetailModel( string? productCode, string? productName, string? standard, string?  classification, double? quantity, string?  unit, string? parentId, string? trajectory , string? size1 , string? size2, string? specification, int? order, string? modeNumber )
    {
      ProductCode = productCode ?? string.Empty;
      ProductName = productName ?? string.Empty;
      Standard = standard ?? string.Empty;
      Classification = classification ?? string.Empty ;
      Quantity = quantity ?? 0;
      Unit = unit ?? string.Empty ;
      ParentId = parentId ?? string.Empty;
      Trajectory = trajectory ?? string.Empty ;
      Size1 = size1 ?? string.Empty ;
      Size2 = size2 ?? string.Empty ;
      Specification = specification ?? string.Empty ;
      Order = order ?? 1 ;
      ModeNumber = modeNumber ?? string.Empty ;
    }
  }
}