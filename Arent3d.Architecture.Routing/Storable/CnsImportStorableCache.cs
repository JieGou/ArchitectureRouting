using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class CnsImportStorableCache : StorableCache<CnsImportStorableCache, CnsImportStorable>
  {
    public CnsImportStorableCache( Document document ) : base( document )
    {
    }

    protected override CnsImportStorable CreateNewStorable( Document document, string name ) => new CnsImportStorable( document ) ;
  }
}