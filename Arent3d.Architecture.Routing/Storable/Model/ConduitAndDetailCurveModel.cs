namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class ConduitAndDetailCurveModel
  {
    public string ConduitId { get ; set ; }
    public string DetailCurveId { get ; set ; }
    public string WireType { get ; set ; }
  
    public ConduitAndDetailCurveModel(string? conduitId, string? detailCurveId, string? wireType )
    {
      ConduitId = conduitId ?? string.Empty ;
      DetailCurveId = detailCurveId ?? string.Empty ;
      WireType = wireType ?? string.Empty ;
    }
  }
}