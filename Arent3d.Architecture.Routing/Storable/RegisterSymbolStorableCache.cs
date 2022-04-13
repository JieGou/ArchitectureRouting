using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class RegisterSymbolStorableCache : StorableCache<RegisterSymbolStorableCache, RegisterSymbolStorable>
  {
    public RegisterSymbolStorableCache( Document document ) : base( document )
    {
      
    }

    protected override RegisterSymbolStorable CreateNewStorable( Document document, string name ) => new RegisterSymbolStorable( document ) ;
  }
}