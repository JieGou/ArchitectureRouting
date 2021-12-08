using System.ComponentModel ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ConduitInformationModel
  {
    [DisplayName("計算除外")]
    public bool? CalculationExclusion { get ; set ; }
    [DisplayName("フロア")]
    public string? Floor { get ; set ; }
    [DisplayName("明細記号")]
    public string? DetailSymbol { get ; set ; }
    [DisplayName("電線種類")]
    public string? WireType { get ; set ; }
    [DisplayName("電線サイズ")]
    public string? WireSize { get ; set ; }
    [DisplayName("電線条数")]
    public string? WireStrip { get ; set ; }
    [DisplayName("電線本数")]
    public string? WireBook { get ; set ; }
    [DisplayName("アース種類")]
    public string? EarthType { get ; set ; }
    [DisplayName("アースサイズ")]
    public string? EarthSize { get ; set ; }
    [DisplayName("アース本数")]
    public string? NumberOfGrounds { get ; set ; }
    [DisplayName("配管種類")]
    public string? PipingType { get ; set ; }
    [DisplayName("配管サイズ")]
    public string? PipingSize { get ; set ; }
    [DisplayName("配管本数")]
    public string? NumberOfPipes { get ; set ; }
    [DisplayName("施工区分")]
    public string? ConstructionClassification { get ; set ; }
    [DisplayName("信号種別")]
    public string? Classification { get ; set ; }
    [DisplayName("工事項目")]
    public string? ConstructionItems { get ; set ; }
    [DisplayName("配管工事項目")]
    public string? PlumbingItems { get ; set ; }
    [DisplayName("備考")]
    public string? Remark { get ; set ; }

    public ConduitInformationModel( 
      bool? calculationExclusion, 
      string? floor, 
      string? detailSymbol,
      string? wireType, 
      string? wireSize, 
      string? wireStrip, 
      string? wireBook,
      string? earthType,
      string? earthSize,
      string? numberOfGrounds,
      string? pipingType,
      string? pipingSize,
      string? numberOfPipes,
      string? constructionClassification,
      string? classification,
      string? constructionItems,
      string? plumbingItems,
      string? remark)
    {
      CalculationExclusion = calculationExclusion ;
      Floor = floor ;
      DetailSymbol = detailSymbol ;
      WireType = wireType ;
      WireSize = wireSize ;
      WireStrip = wireStrip ;
      WireBook = wireBook ;
      EarthType = earthType ;
      EarthSize = earthSize ;
      NumberOfGrounds = numberOfGrounds ;
      PipingType = pipingType ;
      PipingSize = pipingSize ;
      NumberOfPipes = numberOfPipes ;
      ConstructionClassification = constructionClassification ;
      Classification = classification ;
      ConstructionItems = constructionItems ;
      PlumbingItems = plumbingItems ;
      Remark = remark ;
    }
  }
}