using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class LocationTypeStorableCache : StorableCache<LocationTypeStorableCache, LocationTypeStorable>
  {
    public LocationTypeStorableCache( Document document ) : base( document )
    {
      
    }

    protected override LocationTypeStorable CreateNewStorable( Document document, string name ) => new LocationTypeStorable( document ) ;
  }
}