using System ;

namespace Arent3d.Architecture.Routing.Storable.Model
{ 
  public class CeedDetailModel
  {
    public string ProductCode { get ; set ; }
    public string ProductName { get ; set ; }
    public string Standard { get ; set ; }
    public string Classification { get ; set ; }
    public double Quantity { get ; set ; }
    public string Unit { get ; set ; }
    public string ParentId { get ; set ; } 
    public string Trajectory { get ; set ; } 

    public CeedDetailModel( string? productCode, string? productName, string? standard, string?  classification, double? quantity, string?  unit, string? parentId, string? trajectory )
    {
      ProductCode = productCode ?? String.Empty;
      ProductName = productName ?? String.Empty;
      Standard = standard ?? String.Empty;
      Classification = classification ?? String.Empty ;
      Quantity = quantity ?? 0;
      Unit = unit ?? String.Empty ;
      ParentId = parentId ?? String.Empty;
      Trajectory = trajectory ?? String.Empty ;
    }
  }
}