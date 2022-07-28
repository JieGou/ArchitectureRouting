using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class WireLengthNotationModelStorableCache: StorableCache<WireLengthNotationModelStorableCache, WireLengthNotationStorable>
  {
    public WireLengthNotationModelStorableCache( Document document ) : base( document )
    {
      
    }
  
    protected override WireLengthNotationStorable CreateNewStorable( Document document, string name ) => new WireLengthNotationStorable( document ) ;
  }
}