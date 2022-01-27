namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ConnectorFamilyTypeModel
  {
    public string Base64Images { get ; set ; }
    public string FamilyTypeName { get ; set ; }
    public string ConnectorFamilyTypeName { get ; set ; }

    public ConnectorFamilyTypeModel( string? base64Images, string? familyTypeName, string? connectorFamilyTypeName )
    {
      Base64Images = base64Images ?? string.Empty ;
      FamilyTypeName = familyTypeName ?? string.Empty ;
      ConnectorFamilyTypeName = connectorFamilyTypeName ?? string.Empty ;
    }
  }
}