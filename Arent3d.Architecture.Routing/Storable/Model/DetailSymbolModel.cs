namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class DetailSymbolModel
  {
    public string DetailSymbol { get ; set ; }
    public string ConduitId { get ; set ; }
    public string FromConnectorId { get ; set ; }
    public string ToConnectorId { get ; set ; }
    public string Code { get ; set ; }

    public DetailSymbolModel( string? detailSymbol, string? conduitId, string? fromConnectorId, string? toConnectorId, string? code )
    {
      DetailSymbol = detailSymbol ?? string.Empty ;
      ConduitId = conduitId ?? string.Empty ;
      FromConnectorId = fromConnectorId ?? string.Empty ;
      ToConnectorId = toConnectorId ?? string.Empty ;
      Code = code ?? string.Empty ;
    }
  }
}