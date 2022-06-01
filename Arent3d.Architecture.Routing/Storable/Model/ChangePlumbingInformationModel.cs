namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ChangePlumbingInformationModel
  {
    public string ConduitId { get ; set ; }
    public string PlumbingType { get ; set ; }
    public string PlumbingSize { get ; set ; }
    public int NumberOfPlumbing { get ; set ; }
    public string ConstructionClassification { get ; set ; }
    public string ConstructionItems { get ; set ; }

    public ChangePlumbingInformationModel( string? conduitId, string? plumbingType, string? plumbingSize, int? numberOfPlumbing, string? constructionClassification, string? constructionItems )
    {
      ConduitId = conduitId ?? string.Empty ;
      PlumbingType = plumbingType ?? string.Empty ;
      PlumbingSize = plumbingSize ?? string.Empty ;
      NumberOfPlumbing = numberOfPlumbing ?? 1 ;
      ConstructionClassification = constructionClassification ?? string.Empty ;
      ConstructionItems = constructionItems ?? string.Empty ;
    }
  }
}