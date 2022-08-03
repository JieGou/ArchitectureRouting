using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "DDFA14C5-744B-49C6-AEAB-A96B5DF4873B", nameof( PickUpModel ) )]
  public class PickUpModel : IDataModel
  {
    public PickUpModel()
    {
      PickUpData = new List<PickUpItemModel>() ;
    }

    [Field( Documentation = "Pick Up Data" )]
    public List<PickUpItemModel> PickUpData { get ; set ; } = new() ;
  }

  [Schema( "CF4DB4C1-71AF-4C23-B382-5CD8008D150D", nameof( PickUpItemModel ) )]
  public class PickUpItemModel : IDataModel
  {
    public PickUpItemModel()
    {
      Item = string.Empty ;
      Floor = string.Empty ;
      ConstructionItems = string.Empty ;
      EquipmentType = string.Empty ;
      ProductName = string.Empty ;
      Use = string.Empty ;
      UsageName = string.Empty ;
      Construction = string.Empty ;
      ModelNumber = string.Empty ;
      Specification = string.Empty ;
      Specification2 = string.Empty ;
      Size = string.Empty ;
      Quantity = string.Empty ;
      Tani = string.Empty ;
      Supplement = string.Empty ;
      Supplement2 = string.Empty ;
      Group = string.Empty ;
      Layer = string.Empty ;
      Classification = string.Empty ;
      Standard = string.Empty ;
      PickUpNumber = string.Empty ;
      Direction = string.Empty ;
      ProductCode = string.Empty ;
      CeedSetCode = string.Empty ;
      DeviceSymbol = string.Empty ;
      Condition = string.Empty ;
      SumQuantity = string.Empty ;
      RouteName = string.Empty ;
      RelatedRouteName = string.Empty ;
      Version = string.Empty ;
      WireBook = string.Empty ;
    }

    public PickUpItemModel( string? item, string? floor, string? constructionItems, string? equipmentType,
      string? productName, string? use, string? usageName, string? construction, string? modelNumber,
      string? specification, string? specification2, string? size, string? quantity, string? tani, string? supplement,
      string? supplement2, string? group, string? layer, string? classification, string? standard, string? pickUpNumber,
      string? direction, string? productCode, string? ceedSetCode, string? deviceSymbol, string? condition, string? sumQuantity, 
      string? routeName = null, string? relatedRouteName = null, string? version = null, string? wireBook = null )
    {
      Item = item ?? string.Empty ;
      Floor = floor ?? string.Empty ;
      ConstructionItems = constructionItems ?? string.Empty ;
      EquipmentType = equipmentType ?? string.Empty ;
      ProductName = productName ?? string.Empty ;
      Use = use ?? string.Empty ;
      UsageName = usageName ?? string.Empty ;
      Construction = construction ?? string.Empty ;
      ModelNumber = modelNumber ?? string.Empty ;
      Specification = specification ?? string.Empty ;
      Specification2 = specification2 ?? string.Empty ;
      Size = size ?? string.Empty ;
      Quantity = quantity ?? string.Empty ;
      Tani = tani ?? string.Empty ;
      Supplement = supplement ?? string.Empty ;
      Supplement2 = supplement2 ?? string.Empty ;
      Group = group ?? string.Empty ;
      Layer = layer ?? string.Empty ;
      Classification = classification ?? string.Empty ;
      Standard = standard ?? string.Empty ;
      PickUpNumber = pickUpNumber ?? string.Empty ;
      Direction = direction ?? string.Empty ;
      ProductCode = productCode ?? string.Empty ;
      CeedSetCode = ceedSetCode ?? string.Empty ;
      DeviceSymbol = deviceSymbol ?? string.Empty ;
      Condition = condition ?? string.Empty ;
      SumQuantity = sumQuantity ?? string.Empty ;
      RouteName = routeName ?? string.Empty ;
      RelatedRouteName = relatedRouteName ?? string.Empty ;
      Version = version ?? string.Empty ;
      WireBook = wireBook ?? string.Empty ;
    }

    [Field( Documentation = "Item" )]
    public string Item { get ; set ; }

    [Field( Documentation = "Floor" )]
    public string Floor { get ; set ; }

    [Field( Documentation = "Construction Items" )]
    public string ConstructionItems { get ; set ; }

    [Field( Documentation = "Equipment Type" )]
    public string EquipmentType { get ; set ; }

    [Field( Documentation = "Product Name" )]
    public string ProductName { get ; set ; }

    [Field( Documentation = "Use" )]
    public string Use { get ; set ; }

    [Field( Documentation = "Usage Name" )]
    public string UsageName { get ; set ; }

    [Field( Documentation = "Construction" )]
    public string Construction { get ; set ; }

    [Field( Documentation = "Model Number" )]
    public string ModelNumber { get ; set ; }

    [Field( Documentation = "Specification" )]
    public string Specification { get ; set ; }

    [Field( Documentation = "Specification 2" )]
    public string Specification2 { get ; set ; }

    [Field( Documentation = "Size" )]
    public string Size { get ; set ; }

    [Field( Documentation = "Quantity" )]
    public string Quantity { get ; set ; }

    [Field( Documentation = "Tani" )]
    public string Tani { get ; set ; }

    [Field( Documentation = "Supplement" )]
    public string Supplement { get ; set ; }

    [Field( Documentation = "Supplement 2" )]
    public string Supplement2 { get ; set ; }

    [Field( Documentation = "Group" )]
    public string Group { get ; set ; }

    [Field( Documentation = "Layer" )]
    public string Layer { get ; set ; }

    [Field( Documentation = "Classification" )]
    public string Classification { get ; set ; }

    [Field( Documentation = "Standard" )]
    public string Standard { get ; set ; }

    [Field( Documentation = "Pick Up Number" )]
    public string PickUpNumber { get ; set ; }

    [Field( Documentation = "Direction" )]
    public string Direction { get ; set ; }

    [Field( Documentation = "Product Code" )]
    public string ProductCode { get ; set ; }

    [Field( Documentation = "Ceed SetCode" )]
    public string CeedSetCode { get ; set ; }

    [Field( Documentation = "Device Symbol" )]
    public string DeviceSymbol { get ; set ; }

    [Field( Documentation = "Condition" )]
    public string Condition { get ; set ; }
    
    [Field( Documentation = "SumQuantity" )]
    public string SumQuantity { get ; set ; }

    [Field( Documentation = "Route Name" )]
    public string RouteName { get ; set ; }
    
    [Field( Documentation = "Related Route Name" )]
    public string RelatedRouteName { get ; set ; }
    
    [Field( Documentation = "Version" )]
    public string Version { get ; set ; }
    
    [Field( Documentation = "WireBook" )]
    public string WireBook { get ; set ; }
  }
}