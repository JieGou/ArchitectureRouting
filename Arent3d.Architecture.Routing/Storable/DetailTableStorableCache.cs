using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class DetailTableStorableCache : StorableCache<DetailTableStorableCache, DetailTableStorable>
  {
    public DetailTableStorableCache( Document document ) : base( document )
    {
    }

    protected override DetailTableStorable CreateNewStorable( Document document, string name ) => new DetailTableStorable( document ) ;
  }
}