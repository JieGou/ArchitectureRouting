using Arent3d.Revit ;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Storable
{
  public class DemoStorableCache : StorableCache<DemoStorableCache, DemoStorable>
  {
    public DemoStorableCache( Document document ) : base( document )
    {
      
    }

    protected override DemoStorable CreateNewStorable( Document document, string name ) => new DemoStorable( document ) ;
  }
}