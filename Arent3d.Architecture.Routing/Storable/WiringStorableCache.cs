using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class WiringStorableCache : StorableCache<WiringStorableCache, WiringStorable>
  {
    protected override WiringStorable CreateNewStorable( Document document, string name ) => new WiringStorable( document ) ;

    public WiringStorableCache( Document document ) : base( document )
    {
    }
  }
}