namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ConnectorFamilyTypeModel
  {
    public string Base64Images { get ; set ; }
    public string FloorPlanType { get ; set ; }
    public string ConnectorFamilyTypeName { get ; set ; }

    public ConnectorFamilyTypeModel( string? base64Images, string? floorPlanType, string? connectorFamilyTypeName )
    {
      Base64Images = base64Images ?? string.Empty ;
      FloorPlanType = floorPlanType ?? string.Empty ;
      ConnectorFamilyTypeName = connectorFamilyTypeName ?? string.Empty ;
    }
  }
}