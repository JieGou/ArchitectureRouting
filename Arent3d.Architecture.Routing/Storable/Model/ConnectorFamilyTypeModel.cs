namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ConnectorFamilyTypeModel
  {
    public string Base64Images { get ; set ; }
    public string ConnectorFamilyType { get ; set ; }

    public ConnectorFamilyTypeModel( string? base64Images, string? connectorFamilyType )
    {
      Base64Images = base64Images ?? string.Empty ;
      ConnectorFamilyType = connectorFamilyType ?? string.Empty ;
    }
  }
}