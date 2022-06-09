namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ChangePlumbingInformationModel
  {
    public string ConduitId { get ; set ; }
    public string PlumbingType { get ; set ; }
    public string PlumbingSize { get ; set ; }
    public string NumberOfPlumbing { get ; set ; }
    public string ConstructionClassification { get ; set ; }
    public string ConstructionItems { get ; set ; }
    public double WireCrossSectionalArea { get ; set ; }
    public bool IsExposure { get ; set ; }
    public bool IsInDoor { get ; set ; }
    

    public ChangePlumbingInformationModel( string? conduitId, string? plumbingType, string? plumbingSize, string? numberOfPlumbing, string? constructionClassification, string? constructionItems, double? wireCrossSectionalArea, bool? isExposure, bool? isInDoor )
    {
      ConduitId = conduitId ?? string.Empty ;
      PlumbingType = plumbingType ?? string.Empty ;
      PlumbingSize = plumbingSize ?? string.Empty ;
      NumberOfPlumbing = numberOfPlumbing ?? string.Empty ;
      ConstructionClassification = constructionClassification ?? string.Empty ;
      ConstructionItems = constructionItems ?? string.Empty ;
      WireCrossSectionalArea = wireCrossSectionalArea ?? 0 ;
      IsExposure = isExposure ?? false ;
      IsInDoor = isInDoor ?? true ;
    }
  }
}