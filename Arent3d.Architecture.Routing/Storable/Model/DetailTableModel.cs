using System.ComponentModel ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class DetailTableModel
  {
    public bool? CalculationExclusion { get ; set ; }
    public string? Floor { get ; set ; }
    public string? CeeDCode { get ; set ; }
    public string? DetailSymbol { get ; set ; }
    public string? DetailSymbolId { get ; set ; }
    public string? WireType { get ; set ; }
    public string? WireSize { get ; set ; }
    public string? WireStrip { get ; set ; }
    public string? WireBook { get ; set ; }
    public string? EarthType { get ; set ; }
    public string? EarthSize { get ; set ; }
    public string? NumberOfGrounds { get ; set ; }
    public string? PlumbingType { get ; set ; }
    public string? PlumbingSize { get ; set ; }
    public string? NumberOfPlumbing { get ; set ; }
    public string? ConstructionClassification { get ; set ; }
    public string? Classification { get ; set ; }
    public string? ConstructionItems { get ; set ; }
    public string? PlumbingItems { get ; set ; }
    public string? Remark { get ; set ; }
    public double PlumbingCrossSectionalArea { get ; set ; }
    public int CountCableSamePosition { get ; set ; }
    public string? RouteName { get ; set ; }
    public string? IsEcoMode { get ; set ; }
    public bool IsParentRoute { get ; set ; }
    public bool IsReadOnly { get ; set ; }

    public DetailTableModel( 
      bool? calculationExclusion, 
      string? floor, 
      string? ceeDCode,
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
      string? classification,
      string? constructionItems,
      string? plumbingItems,
      string? remark,
      double plumbingCrossSectionalArea,
      int countCableSamePosition,
      string? routeName,
      string? isEcoMode,
      bool isParentRoute,
      bool isReadOnly)
    {
      CalculationExclusion = calculationExclusion ;
      Floor = floor ;
      CeeDCode = ceeDCode ;
      DetailSymbol = detailSymbol ;
      DetailSymbolId = detailSymbolId ;
      WireType = wireType ;
      WireSize = wireSize ;
      WireStrip = wireStrip ;
      WireBook = wireBook ;
      EarthType = earthType ;
      EarthSize = earthSize ;
      NumberOfGrounds = numberOfGrounds ;
      PlumbingType = plumbingType ;
      PlumbingSize = plumbingSize ;
      NumberOfPlumbing = numberOfPlumbing ;
      ConstructionClassification = constructionClassification ;
      Classification = classification ;
      ConstructionItems = constructionItems ;
      PlumbingItems = plumbingItems ;
      Remark = remark ;
      PlumbingCrossSectionalArea = plumbingCrossSectionalArea ;
      CountCableSamePosition = countCableSamePosition ;
      RouteName = routeName ;
      IsEcoMode = isEcoMode ;
      IsParentRoute = isParentRoute ;
      IsReadOnly = isReadOnly ;
    }
  }
}