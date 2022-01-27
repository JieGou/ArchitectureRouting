using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class ConnectorFamilyTypeStorableCache : StorableCache<ConnectorFamilyTypeStorableCache, ConnectorFamilyTypeStorable>
  {
    public ConnectorFamilyTypeStorableCache( Document document ) : base( document )
    {
    }

    protected override ConnectorFamilyTypeStorable CreateNewStorable( Document document, string name ) => new ConnectorFamilyTypeStorable( document ) ;
  }
}