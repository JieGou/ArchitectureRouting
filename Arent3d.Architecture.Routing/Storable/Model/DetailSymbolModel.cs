namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class DetailSymbolModel
  {
    public string DetailSymbolId { get ; set ; }
    public string DetailSymbol { get ; set ; }
    public string ConduitId { get ; set ; }
    public string FromConnectorId { get ; set ; }
    public string ToConnectorId { get ; set ; }
    public string Code { get ; set ; }
    public string LineIds { get ; set ; }
    public int ParentSymbol { get ; set ; }

    public DetailSymbolModel( string? detailSymbolId, string? detailSymbol, string? conduitId, string? fromConnectorId, string? toConnectorId, string? code, string? lineIds, int? parentSymbol )
    {
      DetailSymbolId = detailSymbolId ?? string.Empty ;
      DetailSymbol = detailSymbol ?? string.Empty ;
      ConduitId = conduitId ?? string.Empty ;
      FromConnectorId = fromConnectorId ?? string.Empty ;
      ToConnectorId = toConnectorId ?? string.Empty ;
      Code = code ?? string.Empty ;
      LineIds = lineIds ?? string.Empty ;
      ParentSymbol = parentSymbol ?? 0 ;
    }
  }
}