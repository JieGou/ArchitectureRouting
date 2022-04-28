using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class EcoSettingStorableCache : StorableCache<EcoSettingStorableCache, EcoSettingStorable>
  {
    public EcoSettingStorableCache( Document document ) : base( document )
    {
    }

    protected override EcoSettingStorable CreateNewStorable( Document document, string name ) => new EcoSettingStorable( document ) ;
  }
}