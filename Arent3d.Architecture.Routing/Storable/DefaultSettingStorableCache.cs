using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class DefaultSettingStorableCache : StorableCache<DefaultSettingStorableCache, DefaultSettingStorable>
  {
    public DefaultSettingStorableCache( Document document ) : base( document )
    {
    }

    protected override DefaultSettingStorable CreateNewStorable( Document document, string name ) => new( document ) ;
  }
}