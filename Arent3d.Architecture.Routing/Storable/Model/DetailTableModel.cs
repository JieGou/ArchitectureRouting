using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Runtime.CompilerServices ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public sealed class DetailTableModel : INotifyPropertyChanged
  {
    public bool CalculationExclusion { get ; set ; }
    public string Floor { get ; set ; }
    public string CeedCode { get ; set ; }
    public string DetailSymbol { get ; set ; }
    public string DetailSymbolId { get ; set ; }
    public string WireType { get ; set ; }
    public string WireSize { get ; set ; }
    public string WireStrip { get ; set ; }
    public string WireBook { get ; set ; }
    public string EarthType { get ; set ; }
    public string EarthSize { get ; set ; }
    public string NumberOfGrounds { get ; set ; }
    public string PlumbingType { get ; set ; }
    public string PlumbingSize { get ; set ; }
    public string NumberOfPlumbing { get ; set ; }
    public string ConstructionClassification { get ; set ; }
    public string SignalType { get ; set ; }
    public string ConstructionItems { get ; set ; }
    public string PlumbingItems { get ; set ; }
    public string Remark { get ; set ; }
    public double WireCrossSectionalArea { get ; set ; }
    public int CountCableSamePosition { get ; set ; }
    public string RouteName { get ; set ; }
    public string IsEcoMode { get ; set ; }
    public bool IsParentRoute { get ; set ; }
    public bool IsReadOnly { get ; set ; }
    public string PlumbingIdentityInfo { get ; set ; }
    public string GroupId { get ; set ; }
    public bool IsReadOnlyPlumbingItems { get ; set ; }
    public bool IsMixConstructionItems { get ; set ; }
    public string CopyIndex { get ; set ; }
    public bool IsReadOnlyParameters { get ; set ; }
    public bool IsReadOnlyWireSizeAndWireStrip { get ; set ; }
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

    public DetailTableModel( 
      bool? calculationExclusion, 
      string? floor, 
      string? ceedCode,
      string? detailSymbol,
      string? detailSymbolId,
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
      bool? isReadOnlyPlumbingSize )
    {
      CalculationExclusion = calculationExclusion ?? false ;
      Floor = floor ?? string.Empty ;
      CeedCode = ceedCode ?? string.Empty ;
      DetailSymbol = detailSymbol ?? string.Empty ;
      DetailSymbolId = detailSymbolId ?? string.Empty ;
      WireType = wireType ?? string.Empty ;
      WireSize = wireSize ?? string.Empty ;
      WireStrip = wireStrip ?? string.Empty ;
      WireBook = wireBook ?? string.Empty ;
      EarthType = earthType ?? string.Empty ;
      EarthSize = earthSize ?? string.Empty ;
      NumberOfGrounds = numberOfGrounds ?? string.Empty ;
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
      IsReadOnlyParameters = isReadOnlyParameters ?? true ;
      IsReadOnlyWireSizeAndWireStrip = isReadOnlyWireSizeAndWireStrip ?? true ;
      IsReadOnlyPlumbingSize = isReadOnlyPlumbingSize ?? true ;
    }
    
    public DetailTableModel( 
      bool? calculationExclusion, 
      string? floor, 
      string? ceedCode,
      string? detailSymbol,
      string? detailSymbolId,
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
      List<ComboboxItemType> wireSizes,
      List<ComboboxItemType> wireStrips,
      List<ComboboxItemType> earthSizes,
      List<ComboboxItemType> plumbingSizes )
    {
      CalculationExclusion = calculationExclusion ?? false ;
      Floor = floor ?? string.Empty ;
      CeedCode = ceedCode ?? string.Empty ;
      DetailSymbol = detailSymbol ?? string.Empty ;
      DetailSymbolId = detailSymbolId ?? string.Empty ;
      WireType = wireType ?? string.Empty ;
      WireSize = wireSize ?? string.Empty ;
      WireStrip = wireStrip ?? string.Empty ;
      WireBook = wireBook ?? string.Empty ;
      EarthType = earthType ?? string.Empty ;
      EarthSize = earthSize ?? string.Empty ;
      NumberOfGrounds = numberOfGrounds ?? string.Empty ;
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
      IsReadOnlyParameters = isReadOnlyParameters ?? true ;
      IsReadOnlyWireSizeAndWireStrip = isReadOnlyWireSizeAndWireStrip ?? true ;
      IsReadOnlyPlumbingSize = isReadOnlyPlumbingSize ?? true ;
      WireSizes = wireSizes ;
      WireStrips = wireStrips ;
      EarthSizes = earthSizes ;
      PlumbingSizes = plumbingSizes ;
    }
    
    public DetailTableModel( string? detailSymbol, string? detailSymbolId)
    {
      var index = "new-" + DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      CalculationExclusion = false ;
      Floor = string.Empty ;
      CeedCode = string.Empty ;
      DetailSymbol = detailSymbol ?? string.Empty ;
      DetailSymbolId = detailSymbolId ?? string.Empty ;
      WireType = string.Empty ;
      WireSize = string.Empty ;
      WireStrip = string.Empty ;
      WireBook = string.Empty ;
      EarthType = string.Empty ;
      EarthSize = string.Empty ;
      NumberOfGrounds = string.Empty ;
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
      RouteName = index ;
      IsEcoMode = string.Empty ;
      IsParentRoute = true ;
      IsReadOnly = false ;
      PlumbingIdentityInfo = index ;
      GroupId = string.Empty ;
      IsReadOnlyPlumbingItems = false ;
      IsMixConstructionItems = false ;
      CopyIndex = string.Empty ;
      IsReadOnlyParameters = false ;
      IsReadOnlyWireSizeAndWireStrip = false ;
      IsReadOnlyPlumbingSize = false ;
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

    public event PropertyChangedEventHandler? PropertyChanged ;

    private void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
    {
      PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
    }
  }
}