using System ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class WiringInformationChangedModel
  {
    public string ConnectorUniqueId { get ; }
    public string MaterialCode { get ; set ; }

    public WiringInformationChangedModel( string? connectorUniqueId, string? materialCode )
    {
      ConnectorUniqueId = connectorUniqueId ?? String.Empty ;
      MaterialCode = materialCode ?? String.Empty;
    }
  }
}