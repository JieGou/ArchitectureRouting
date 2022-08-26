using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema("102552D3-F0F7-4E7E-8F24-19D2AE53178C", nameof(DetailSymbolModel))]
  public class DetailSymbolModel : IDataModel
  {
    [Field(Documentation = "Detail Symbol Data")]
    public List<DetailSymbolItemModel> DetailSymbolData { get ; set ; }

    public DetailSymbolModel()
    {
      DetailSymbolData = new List<DetailSymbolItemModel>() ;
    }
  }
  
  [Schema("B5559140-7292-4AB8-9799-1EC6212A991A", nameof(DetailSymbolItemModel))]
  public class DetailSymbolItemModel : IDataModel
  {
    [Field(Documentation = "Detail Symbol UniqueId")]
    public string DetailSymbolUniqueId { get ; set ; }
    
    [Field(Documentation = "From Connector UniqueId")]
    public string FromConnectorUniqueId { get ; set ; }
    
    [Field(Documentation = "To Connector UniqueId")]
    public string ToConnectorUniqueId { get ; set ; }
    
    [Field(Documentation = "Detail Symbol")]
    public string DetailSymbol { get ; set ; }
    
    [Field(Documentation = "Conduit UniqueId")]
    public string ConduitUniqueId { get ; set ; }
    
    [Field(Documentation = "Route Name")]
    public string RouteName { get ; set ; }
    
    [Field(Documentation = "Code")]
    public string Code { get ; set ; }
    
    [Field(Documentation = "Lines UniqueId")]
    public string LineUniqueIds { get ; set ; }
    
    [Field(Documentation = "Is Parent Symbol")]
    public bool IsParentSymbol { get ; set ; }
    
    [Field(Documentation = "Count Cable Same Position")]
    public int CountCableSamePosition { get ; set ; }
    
    [Field(Documentation = "Device Symbol")]
    public string DeviceSymbol { get ; set ; }
    
    [Field(Documentation = "Plumbing Type")]
    public string PlumbingType { get ; set ; }

    public DetailSymbolItemModel()
    {
      DetailSymbol = string.Empty ;
      DetailSymbolUniqueId = string.Empty ;
      FromConnectorUniqueId = string.Empty ;
      ToConnectorUniqueId = string.Empty ;
      ConduitUniqueId = string.Empty ;
      RouteName = string.Empty ;
      Code = string.Empty ;
      LineUniqueIds = string.Empty ;
      IsParentSymbol = true ;
      CountCableSamePosition = 1 ;
      DeviceSymbol = string.Empty ;
      PlumbingType = string.Empty ;
    }

    public DetailSymbolItemModel( string? detailSymbol, string? detailSymbolUniqueId, string? fromConnectorUniqueId, string? toConnectorUniqueId, string? conduitUniqueId, string? routeName, string? code, string? lineUniqueIds, bool? isParentSymbol, int? countCableSamePosition, string? deviceSymbol, string? plumbingType )
    {
      DetailSymbol = detailSymbol ?? string.Empty ;
      DetailSymbolUniqueId = detailSymbolUniqueId ?? string.Empty ;
      FromConnectorUniqueId = fromConnectorUniqueId ?? string.Empty ;
      ToConnectorUniqueId = toConnectorUniqueId ?? string.Empty ;
      ConduitUniqueId = conduitUniqueId ?? string.Empty ;
      RouteName = routeName ?? string.Empty ;
      Code = code ?? string.Empty ;
      LineUniqueIds = lineUniqueIds ?? string.Empty ;
      IsParentSymbol = isParentSymbol ?? true ;
      CountCableSamePosition = countCableSamePosition ?? 1 ;
      DeviceSymbol = deviceSymbol ?? string.Empty ;
      PlumbingType = plumbingType ?? string.Empty ;
    }
  }
}