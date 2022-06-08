using System ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class WiringModel
  {
    public string Id { get ; set ; }
    public string IdOfToConnector { get ; set ; }
    public string RouteName { get ; set ; }
    public string Floor { get ; set ; }
    public string GeneralDisplayDeviceSymbol { get ; set ; }
    public string WireType { get ; set ; }
    public string WireSize { get ; set ; }
    public string WireStrip { get ; set ; }
    public string PipingType { get ; set ; }
    public string PipingSize { get ; set ; }
    
    public string NumberOfPlumbing { get ; set ; }
    
    public string ConstructionClassification { get ; set ; }
    public string SignalType { get ; set ; }
    public string ConstructionItems { get ; set ; }
    public string PlumbingItems { get ; set ; }
    public string Remark { get ; set ; }
    public string ParentPartMode { get ; set ; }

    public WiringModel( string? id, string? idOfToConnector, string? routeName, string? floor, string? generalDisplayDeviceSymbol, string? wireType, string? wireSize, string? wireStrip, string? pipingType, string? pipingSize, 
      string? numberOfPlumbing, string ? constructionClassification, string? signalType, string? constructionItems, string? plumbingItems, string? remark, string? parentPartMode)
    {
      Id = id ?? string.Empty ;
      IdOfToConnector = idOfToConnector ?? string.Empty ;
      RouteName = routeName ?? string.Empty ;
      Floor = floor ?? string.Empty ;
      GeneralDisplayDeviceSymbol = generalDisplayDeviceSymbol ?? string.Empty ;
      WireType = wireType ?? string.Empty ;
      WireSize = wireSize ?? string.Empty ;
      WireStrip = wireStrip ?? string.Empty ;
      PipingType = pipingType ?? string.Empty ;
      PipingSize = pipingSize ?? string.Empty ;
      NumberOfPlumbing = numberOfPlumbing ?? string.Empty ;
      ConstructionClassification = constructionClassification ?? string.Empty ;
      SignalType = signalType ?? string.Empty ;
      ConstructionItems = constructionItems ?? string.Empty ;
      PlumbingItems = plumbingItems ?? string.Empty ;
      Remark = remark ?? string.Empty ;
      ParentPartMode = parentPartMode ?? string.Empty ;
    }
  }
}