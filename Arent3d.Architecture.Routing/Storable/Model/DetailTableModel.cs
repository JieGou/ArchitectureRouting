namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class DetailTableModel
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
      string? copyIndex)
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
    }
  }
}