using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class SymbolInformationStorableCache : StorableCache<SymbolInformationStorableCache, SymbolInformationStorable>
  {
    public SymbolInformationStorableCache( Document document ) : base( document )
    {
    }

    protected override SymbolInformationStorable CreateNewStorable( Document document, string name ) => new SymbolInformationStorable( document ) ;
  }
}