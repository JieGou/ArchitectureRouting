using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  public class ConduitAndDetailCurveStorableCache : StorableCache<ConduitAndDetailCurveStorableCache, ConduitAndDetailCurveStorable>
  {
    public ConduitAndDetailCurveStorableCache( Document document ) : base( document )
    {
    }

    protected override ConduitAndDetailCurveStorable CreateNewStorable( Document document, string name ) => new ConduitAndDetailCurveStorable( document ) ;
  }
}