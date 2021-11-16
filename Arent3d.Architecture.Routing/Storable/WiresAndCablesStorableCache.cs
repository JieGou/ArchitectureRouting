using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class WiresAndCablesStorableCache : StorableCache<WiresAndCablesStorableCache, WiresAndCablesStorable>
  {
    public WiresAndCablesStorableCache( Document document ) : base( document )
    {
    }

    protected override WiresAndCablesStorable CreateNewStorable( Document document, string name ) => new WiresAndCablesStorable( document ) ;
  }
}