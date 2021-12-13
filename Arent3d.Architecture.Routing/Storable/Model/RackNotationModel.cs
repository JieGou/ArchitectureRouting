namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class RackNotationModel
  {
    public string RackId { get ; set ; }
    public string NotationId { get ; set ; }
    public string RackNotationId { get ; set ; }
    public string FromConnectorId { get ; set ; }
    public bool IsDirectionX { get ; set ; }
    public double RackWidth { get ; set ; }
    public string LineIds { get ; set ; }
     

    public RackNotationModel( string? rackId, string? notationId, string? rackNotationId, string? fromConnectorId, bool? isDirectionX, double? rackWidth, string? lineIds )
    {
      RackId = rackId ?? string.Empty ;
      NotationId = notationId ?? string.Empty ;
      RackNotationId = rackNotationId ?? string.Empty ;
      FromConnectorId = fromConnectorId ?? string.Empty ;
      IsDirectionX = isDirectionX ?? false ;
      RackWidth = rackWidth ?? 0 ;
      LineIds = lineIds ?? string.Empty ;
    }
  }
}