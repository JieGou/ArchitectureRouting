using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class PressureGuidingTubeStorableCache : StorableCache<PressureGuidingTubeStorableCache, PressureGuidingTubeStorable>
  {
    public PressureGuidingTubeStorableCache( Document document ) : base( document )
    {
    }

    protected override PressureGuidingTubeStorable CreateNewStorable( Document document, string name ) => new PressureGuidingTubeStorable( document ) ;
  }
}