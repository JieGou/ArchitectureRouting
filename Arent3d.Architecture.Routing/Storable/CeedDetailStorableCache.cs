using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class CeedDetailStorableCache : StorableCache<CeedDetailStorableCache, CeedDetailStorable>
  {
    public CeedDetailStorableCache( Document document ) : base( document )
    {
    }

    protected override CeedDetailStorable CreateNewStorable( Document document, string name ) => new CeedDetailStorable( document ) ;
  }
}