using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class WiringInformationChangedStorableCache : StorableCache<WiringInformationChangedStorableCache, WiringInformationChangedStorable>
  {
    protected override WiringInformationChangedStorable CreateNewStorable( Document document, string name ) => new WiringInformationChangedStorable( document ) ;

    public WiringInformationChangedStorableCache( Document document ) : base( document )
    {
    }
  }
}