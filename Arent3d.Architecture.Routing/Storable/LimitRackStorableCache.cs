using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class LimitRackStorableCache : StorableCache<LimitRackStorableCache, LimitRackStorable>
  {
    public LimitRackStorableCache( Document document ) : base( document )
    {
    }

    protected override LimitRackStorable CreateNewStorable( Document document, string name ) => new(document) ;
  }
}