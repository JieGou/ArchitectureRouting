using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages.Attributes ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "95677FD8-B2AA-49DC-9D2D-3C6B4B83FB04", nameof( DetailTableModel ) )]
  public class DetailTableModel : IDataModel
  {
    [Field(Documentation = "Detail Table Data")]
    public List<DetailTableItemModel> DetailTableData { get ; set ; }

    public DetailTableModel()
    {
      DetailTableData = new List<DetailTableItemModel>() ;
    }
  }
  
  [Schema( "C403896F-A525-4AEB-910E-F770D54C5CBD" , nameof( DetailTableItemModel ))]
  public sealed class DetailTableItemModel : NotifyPropertyChanged, IDataModel
  {
    [Field(Documentation = "Calculation Exclusion")]
    public bool CalculationExclusion { get ; set ; }
    
    [Field(Documentation = "Floor")]
    public string Floor { get ; set ; }
    
    [Field(Documentation = "Ceed Code")]
    public string CeedCode { get ; set ; }
    
    [Field(Documentation = "Detail Symbol")]
    public string DetailSymbol { get ; set ; }
    
    [Field(Documentation = "Detail Symbol UniqueId")]
    public string DetailSymbolUniqueId { get ; set ; }
    
    [Field(Documentation = "From Connector UniqueId")]
    public string FromConnectorUniqueId { get ; set ; }
    
    [Field(Documentation = "To Connector UniqueId")]
    public string ToConnectorUniqueId { get ; set ; }
    
    [Field(Documentation = "Wire Type")]
    public string WireType { get ; set ; }
    
    [Field(Documentation = "Wire Size")]
    public string WireSize { get ; set ; }
    
    [Field(Documentation = "Wire Strip")]
    public string WireStrip { get ; set ; }
    
    [Field(Documentation = "Wire Book")]
    public string WireBook { get ; set ; }
    
    [Field(Documentation = "Earth Type")]
    public string EarthType { get ; set ; }
    
    [Field(Documentation = "Earth Size")]
    public string EarthSize { get ; set ; }
    
    [Field(Documentation = "Number Of Ground")]
    public string NumberOfGround { get ; set ; }
    
    [Field(Documentation = "Plumbing Type")]
    public string PlumbingType { get ; set ; }
    
    [Field(Documentation = "Plumbing Size")]
    public string PlumbingSize { get ; set ; }
    
    [Field(Documentation = "Number Of Plumbing")]
    public string NumberOfPlumbing { get ; set ; }
    
    [Field(Documentation = "Construction Classification")]
    public string ConstructionClassification { get ; set ; }
    
    [Field(Documentation = "Signal Type")]
    public string SignalType { get ; set ; }
    
    [Field(Documentation = "Construction Items")]
    public string ConstructionItems { get ; set ; }
    
    [Field(Documentation = "Plumbing Items")]
    public string PlumbingItems { get ; set ; }
    
    [Field(Documentation = "Remark")]
    public string Remark { get ; set ; }
    
    [Field(Documentation = "Wire Cross Sectional Area", SpecTypeId = SpecTypeCode.Area, UnitTypeId = UnitTypeCode.SquareMillimeters)]
    public double WireCrossSectionalArea { get ; set ; }
    
    [Field(Documentation = "Count Cable Same Position")]
    public int CountCableSamePosition { get ; set ; }
    
    [Field(Documentation = "Route Name")]
    public string RouteName { get ; set ; }
    
    [Field(Documentation = "Is Eco Mode")]
    public string IsEcoMode { get ; set ; }
    
    [Field(Documentation = "Is Parent Route")]
    public bool IsParentRoute { get ; set ; }
    
    [Field(Documentation = "Is ReadOnly")]
    public bool IsReadOnly { get ; set ; }
    
    [Field(Documentation = "Plumbing Identity Info")]
    public string PlumbingIdentityInfo { get ; set ; }
    
    [Field(Documentation = "GroupId")]
    public string GroupId { get ; set ; }
    public bool IsGrouped { get ; set ; }
    
    [Field(Documentation = "Is ReadOnly Plumbing Items")]
    public bool IsReadOnlyPlumbingItems { get ; set ; }
    
    [Field(Documentation = "Is Mix Construction Items")]
    public bool IsMixConstructionItems { get ; set ; }
    
    [Field(Documentation = "Copy Index")]
    public string CopyIndex { get ; set ; }
    
    [Field(Documentation = "Is ReadOnly Parameters")]
    public bool IsReadOnlyParameters { get ; set ; }
    
    [Field(Documentation = "Is ReadOnly Wire Size And Wire Strip")]
    public bool IsReadOnlyWireSizeAndWireStrip { get ; set ; }
    
    [Field(Documentation = "Is ReadOnly Plumbing Size")]
    public bool IsReadOnlyPlumbingSize { get ; set ; }
    
    private List<ComboboxItemType> _wireSizes = new() ;
    public List<ComboboxItemType> WireSizes
    {
      get => _wireSizes ;
      set
      {
        _wireSizes = value ;
        OnPropertyChanged( nameof( WireSizes ) ) ;
      }
    } 
    
    private List<ComboboxItemType> _wireStrips = new() ;
    public List<ComboboxItemType> WireStrips
    {
      get => _wireStrips ;
      set
      {
        _wireStrips = value ;
        OnPropertyChanged( nameof( WireStrips ) ) ;
      }
    }
    
    private List<ComboboxItemType> _earthSizes = new() ;
    public List<ComboboxItemType> EarthSizes
    {
      get => _earthSizes ;
      set
      {
        _earthSizes = value ;
        OnPropertyChanged( nameof( EarthSizes ) ) ;
      }
    } 
    
    private List<ComboboxItemType> _plumbingSizes = new() ;
    public List<ComboboxItemType> PlumbingSizes
    {
      get => _plumbingSizes ;
      set
      {
        _plumbingSizes = value ;
        OnPropertyChanged( nameof( PlumbingSizes ) ) ;
      }
    }
    
    private List<ComboboxItemType> _plumbingItems = new(){new ComboboxItemType("未設定","未設定")} ;
    public List<ComboboxItemType> PlumbingItemTypes
    {
      get => _plumbingItems ;
      set
      {
        _plumbingItems = value ;
        OnPropertyChanged( nameof( PlumbingItemTypes ) ) ;
      }
    }

    public DetailTableItemModel()
    {
      CalculationExclusion = false ;
      Floor = string.Empty ;
      CeedCode = string.Empty ;
      DetailSymbol = string.Empty ;
      DetailSymbolUniqueId = string.Empty ;
      FromConnectorUniqueId = string.Empty ;
      ToConnectorUniqueId = string.Empty ;
      WireType = string.Empty ;
      WireSize = string.Empty ;
      WireStrip = string.Empty ;
      WireBook = string.Empty ;
      EarthType = string.Empty ;
      EarthSize = string.Empty ;
      NumberOfGround = string.Empty ;
      PlumbingType = string.Empty ;
      PlumbingSize = string.Empty ;
      NumberOfPlumbing = string.Empty ;
      ConstructionClassification = string.Empty ;
      SignalType = string.Empty ;
      ConstructionItems = string.Empty ;
      PlumbingItems = string.Empty ;
      Remark = string.Empty ;
      WireCrossSectionalArea = 0 ;
      CountCableSamePosition = 1 ;
      RouteName = string.Empty ;
      IsEcoMode = string.Empty ;
      IsParentRoute = false ;
      IsReadOnly = true ;
      PlumbingIdentityInfo = string.Empty ;
      GroupId = string.Empty ;
      IsGrouped = false ;
      IsReadOnlyPlumbingItems = true ;
      IsMixConstructionItems = false ;
      CopyIndex = string.Empty ;
      IsReadOnlyParameters = false ;
      IsReadOnlyWireSizeAndWireStrip = false ;
      IsReadOnlyPlumbingSize = false ;
    }

    public DetailTableItemModel( 
      bool? calculationExclusion, 
      string? floor, 
      string? ceedCode,
      string? detailSymbol,
      string? detailSymbolUniqueId,
      string? fromConnectorUniqueId,
      string? toConnectorUniqueId,
      string? wireType, 
      string? wireSize, 
      string? wireStrip, 
      string? wireBook,
      string? earthType,
      string? earthSize,
      string? numberOfGrounds,
      string? plumbingType,
      string? plumbingSize,
      string? numberOfPlumbing,
      string? constructionClassification,
      string? signalType,
      string? constructionItems,
      string? plumbingItems,
      string? remark,
      double? wireCrossSectionalArea,
      int? countCableSamePosition,
      string? routeName,
      string? isEcoMode,
      bool? isParentRoute,
      bool? isReadOnly,  
      string? plumbingIdentityInfo,
      string? groupId,
      bool? isReadOnlyPlumbingItems,
      bool? isMixConstructionItems,
      string? copyIndex,
      bool? isReadOnlyParameters,
      bool? isReadOnlyWireSizeAndWireStrip,
      bool? isReadOnlyPlumbingSize,
      IEnumerable<string>? wireSizes,
      IEnumerable<string>? wireStrips,
      IEnumerable<string>? earthSizes,
      IEnumerable<string>? plumbingSizes,
      IEnumerable<string>? plumbingItemTypes)
    {
      CalculationExclusion = calculationExclusion ?? false ;
      Floor = floor ?? string.Empty ;
      CeedCode = ceedCode ?? string.Empty ;
      DetailSymbol = detailSymbol ?? string.Empty ;
      DetailSymbolUniqueId = detailSymbolUniqueId ?? string.Empty ;
      FromConnectorUniqueId = fromConnectorUniqueId ?? string.Empty ;
      ToConnectorUniqueId = toConnectorUniqueId ?? string.Empty ;
      WireType = wireType ?? string.Empty ;
      WireSize = wireSize ?? string.Empty ;
      WireStrip = wireStrip ?? string.Empty ;
      WireBook = wireBook ?? string.Empty ;
      EarthType = earthType ?? string.Empty ;
      EarthSize = earthSize ?? string.Empty ;
      NumberOfGround = numberOfGrounds ?? string.Empty ;
      PlumbingType = plumbingType ?? string.Empty ;
      PlumbingSize = plumbingSize ?? string.Empty ;
      NumberOfPlumbing = numberOfPlumbing ?? string.Empty ;
      ConstructionClassification = constructionClassification ?? string.Empty ;
      SignalType = signalType ?? string.Empty ;
      ConstructionItems = constructionItems ?? string.Empty ;
      PlumbingItems = plumbingItems ?? string.Empty ;
      Remark = remark ?? string.Empty ;
      WireCrossSectionalArea = wireCrossSectionalArea ?? 0 ;
      CountCableSamePosition = countCableSamePosition ?? 1 ;
      RouteName = routeName ?? string.Empty ;
      IsEcoMode = isEcoMode ?? string.Empty ;
      IsParentRoute = isParentRoute ?? false ;
      IsReadOnly = isReadOnly ?? true ;
      PlumbingIdentityInfo = plumbingIdentityInfo ?? string.Empty ;
      GroupId = groupId ?? string.Empty ;
      IsReadOnlyPlumbingItems = isReadOnlyPlumbingItems ?? true ;
      IsMixConstructionItems = isMixConstructionItems ?? false ;
      CopyIndex = copyIndex ?? string.Empty ;
      IsReadOnlyParameters = isReadOnlyParameters ?? false ;
      IsReadOnlyWireSizeAndWireStrip = isReadOnlyWireSizeAndWireStrip ?? false ;
      IsReadOnlyPlumbingSize = isReadOnlyPlumbingSize ?? false ;
      WireSizes = ( from wireSizeType in wireSizes select new ComboboxItemType( wireSizeType, wireSizeType ) ).ToList() ;
      WireStrips = ( from wireStripType in wireStrips select new ComboboxItemType( wireStripType, wireStripType ) ).ToList() ;
      EarthSizes = ( from earthSizeType in earthSizes select new ComboboxItemType( earthSizeType, earthSizeType ) ).ToList() ;
      PlumbingSizes = ( from plumbingSizeType in plumbingSizes select new ComboboxItemType( plumbingSizeType, plumbingSizeType ) ).ToList() ;
      PlumbingItemTypes = ( from plumbingItemType in plumbingItemTypes select new ComboboxItemType( plumbingItemType, plumbingItemType ) ).ToList() ;
    }
    
    public DetailTableItemModel( 
      bool? calculationExclusion, 
      string? floor, 
      string? ceedCode,
      string? detailSymbol,
      string? detailSymbolUniqueId,
      string? fromConnectorUniqueId,
      string? toConnectorUniqueId,
      string? wireType, 
      string? wireSize, 
      string? wireStrip, 
      string? wireBook,
      string? earthType,
      string? earthSize,
      string? numberOfGrounds,
      string? plumbingType,
      string? plumbingSize,
      string? numberOfPlumbing,
      string? constructionClassification,
      string? signalType,
      string? constructionItems,
      string? plumbingItems,
      string? remark,
      double? wireCrossSectionalArea,
      int? countCableSamePosition,
      string? routeName,
      string? isEcoMode,
      bool? isParentRoute,
      bool? isReadOnly,  
      string? plumbingIdentityInfo,
      string? groupId,
      bool? isReadOnlyPlumbingItems,
      bool? isMixConstructionItems,
      string? copyIndex,
      bool? isReadOnlyParameters,
      bool? isReadOnlyWireSizeAndWireStrip,
      bool? isReadOnlyPlumbingSize,
      List<ComboboxItemType>? wireSizes,
      List<ComboboxItemType>? wireStrips,
      List<ComboboxItemType>? earthSizes,
      List<ComboboxItemType>? plumbingSizes,
      List<ComboboxItemType>? plumbingItemTypes )
    {
      CalculationExclusion = calculationExclusion ?? false ;
      Floor = floor ?? string.Empty ;
      CeedCode = ceedCode ?? string.Empty ;
      DetailSymbol = detailSymbol ?? string.Empty ;
      DetailSymbolUniqueId = detailSymbolUniqueId ?? string.Empty ;
      FromConnectorUniqueId = fromConnectorUniqueId ?? string.Empty ;
      ToConnectorUniqueId = toConnectorUniqueId ?? string.Empty ;
      WireType = wireType ?? string.Empty ;
      WireSize = wireSize ?? string.Empty ;
      WireStrip = wireStrip ?? string.Empty ;
      WireBook = wireBook ?? string.Empty ;
      EarthType = earthType ?? string.Empty ;
      EarthSize = earthSize ?? string.Empty ;
      NumberOfGround = numberOfGrounds ?? string.Empty ;
      PlumbingType = plumbingType ?? string.Empty ;
      PlumbingSize = plumbingSize ?? string.Empty ;
      NumberOfPlumbing = numberOfPlumbing ?? string.Empty ;
      ConstructionClassification = constructionClassification ?? string.Empty ;
      SignalType = signalType ?? string.Empty ;
      ConstructionItems = constructionItems ?? string.Empty ;
      PlumbingItems = plumbingItems ?? string.Empty ;
      Remark = remark ?? string.Empty ;
      WireCrossSectionalArea = wireCrossSectionalArea ?? 0 ;
      CountCableSamePosition = countCableSamePosition ?? 1 ;
      RouteName = routeName ?? string.Empty ;
      IsEcoMode = isEcoMode ?? string.Empty ;
      IsParentRoute = isParentRoute ?? false ;
      IsReadOnly = isReadOnly ?? true ;
      PlumbingIdentityInfo = plumbingIdentityInfo ?? string.Empty ;
      GroupId = groupId ?? string.Empty ;
      IsReadOnlyPlumbingItems = isReadOnlyPlumbingItems ?? true ;
      IsMixConstructionItems = isMixConstructionItems ?? false ;
      CopyIndex = copyIndex ?? string.Empty ;
      IsReadOnlyParameters = isReadOnlyParameters ?? false ;
      IsReadOnlyWireSizeAndWireStrip = isReadOnlyWireSizeAndWireStrip ?? false ;
      IsReadOnlyPlumbingSize = isReadOnlyPlumbingSize ?? false ;
      WireSizes = wireSizes ?? new List<ComboboxItemType>() ;
      WireStrips = wireStrips ?? new List<ComboboxItemType>() ;
      EarthSizes = earthSizes ?? new List<ComboboxItemType>() ;
      PlumbingSizes = plumbingSizes ?? new List<ComboboxItemType>() ;
      PlumbingItemTypes = plumbingItemTypes ?? new List<ComboboxItemType>() ;
    }
    
    public DetailTableItemModel( string? detailSymbol, string? detailSymbolUniqueId, string? fromConnectorUniqueId, string? toConnectorUniqueId, string routeName)
    {
      CalculationExclusion = false ;
      Floor = string.Empty ;
      CeedCode = string.Empty ;
      DetailSymbol = detailSymbol ?? string.Empty ;
      DetailSymbolUniqueId = detailSymbolUniqueId ?? string.Empty ;
      FromConnectorUniqueId = fromConnectorUniqueId ?? string.Empty ;
      ToConnectorUniqueId = toConnectorUniqueId ?? string.Empty ;
      WireType = string.Empty ;
      WireSize = string.Empty ;
      WireStrip = string.Empty ;
      WireBook = string.Empty ;
      EarthType = string.Empty ;
      EarthSize = string.Empty ;
      NumberOfGround = string.Empty ;
      PlumbingType = string.Empty ;
      PlumbingSize = string.Empty ;
      NumberOfPlumbing = string.Empty ;
      ConstructionClassification = string.Empty ;
      SignalType = string.Empty ;
      ConstructionItems = string.Empty ;
      PlumbingItems = string.Empty ;
      Remark = string.Empty ;
      WireCrossSectionalArea = 0 ;
      CountCableSamePosition = 1 ;
      RouteName = routeName ;
      IsEcoMode = string.Empty ;
      IsParentRoute = true ;
      IsReadOnly = false ;
      PlumbingIdentityInfo = string.Empty ;
      GroupId = string.Empty ;
      IsReadOnlyPlumbingItems = false ;
      IsMixConstructionItems = false ;
      CopyIndex = string.Empty ;
      IsReadOnlyParameters = false ;
      IsReadOnlyWireSizeAndWireStrip = false ;
      IsReadOnlyPlumbingSize = false ;
    }
    
    public DetailTableItemModel( 
      bool? calculationExclusion, 
      string? floor, 
      string? ceedCode,
      string? detailSymbol,
      string? detailSymbolUniqueId,
      string? fromConnectorUniqueId,
      string? toConnectorUniqueId,
      string? wireType, 
      string? wireSize, 
      string? wireStrip, 
      string? wireBook,
      string? earthType,
      string? earthSize,
      string? numberOfGrounds,
      string? plumbingType,
      string? plumbingSize,
      string? numberOfPlumbing,
      string? constructionClassification,
      string? signalType,
      string? constructionItems,
      string? plumbingItems,
      string? remark,
      double? wireCrossSectionalArea,
      int? countCableSamePosition,
      string? routeName,
      string? isEcoMode,
      bool? isParentRoute,
      bool? isReadOnly,  
      string? plumbingIdentityInfo,
      string? groupId,
      bool? isReadOnlyPlumbingItems,
      bool? isMixConstructionItems,
      string? copyIndex )
    {
      CalculationExclusion = calculationExclusion ?? false ;
      Floor = floor ?? string.Empty ;
      CeedCode = ceedCode ?? string.Empty ;
      DetailSymbol = detailSymbol ?? string.Empty ;
      DetailSymbolUniqueId = detailSymbolUniqueId ?? string.Empty ;
      FromConnectorUniqueId = fromConnectorUniqueId ?? string.Empty ;
      ToConnectorUniqueId = toConnectorUniqueId ?? string.Empty ;
      WireType = wireType ?? string.Empty ;
      WireSize = wireSize ?? string.Empty ;
      WireStrip = wireStrip ?? string.Empty ;
      WireBook = wireBook ?? string.Empty ;
      EarthType = earthType ?? string.Empty ;
      EarthSize = earthSize ?? string.Empty ;
      NumberOfGround = numberOfGrounds ?? string.Empty ;
      PlumbingType = plumbingType ?? string.Empty ;
      PlumbingSize = plumbingSize ?? string.Empty ;
      NumberOfPlumbing = numberOfPlumbing ?? string.Empty ;
      ConstructionClassification = constructionClassification ?? string.Empty ;
      SignalType = signalType ?? string.Empty ;
      ConstructionItems = constructionItems ?? string.Empty ;
      PlumbingItems = plumbingItems ?? string.Empty ;
      Remark = remark ?? string.Empty ;
      WireCrossSectionalArea = wireCrossSectionalArea ?? 0 ;
      CountCableSamePosition = countCableSamePosition ?? 1 ;
      RouteName = routeName ?? string.Empty ;
      IsEcoMode = isEcoMode ?? string.Empty ;
      IsParentRoute = isParentRoute ?? false ;
      IsReadOnly = isReadOnly ?? true ;
      PlumbingIdentityInfo = plumbingIdentityInfo ?? string.Empty ;
      GroupId = groupId ?? string.Empty ;
      IsReadOnlyPlumbingItems = isReadOnlyPlumbingItems ?? true ;
      IsMixConstructionItems = isMixConstructionItems ?? false ;
      CopyIndex = copyIndex ?? string.Empty ;
      IsReadOnlyParameters = false ;
      IsReadOnlyWireSizeAndWireStrip = false ;
      IsReadOnlyPlumbingSize = isReadOnly ?? false ;
      WireSizes = new List<ComboboxItemType>() ;
      WireStrips = new List<ComboboxItemType>() ;
      EarthSizes = new List<ComboboxItemType>() ;
      PlumbingSizes = new List<ComboboxItemType>() ;
      PlumbingItemTypes = new List<ComboboxItemType>() ;
    }
    
    public class ComboboxItemType
    {
      public string Type { get ; }
      public string Name { get ; }

      public ComboboxItemType( string type, string name )
      {
        Type = type ;
        Name = name ;
      }
    }
    
  }
}