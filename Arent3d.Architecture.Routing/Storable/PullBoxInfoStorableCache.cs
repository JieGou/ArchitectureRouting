using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class PullBoxInfoStorableCache : StorableCache<PullBoxInfoStorableCache, PullBoxInfoStorable>
  {
    public PullBoxInfoStorableCache( Document document ) : base( document )
    {
    }

    protected override PullBoxInfoStorable CreateNewStorable( Document document, string name )
    {
      return new(document) ;
    }
  }
}