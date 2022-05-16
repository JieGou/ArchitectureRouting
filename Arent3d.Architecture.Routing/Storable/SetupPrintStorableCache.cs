using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class SetupPrintStorableCache : StorableCache<SetupPrintStorableCache, SetupPrintStorable>
  {
    public SetupPrintStorableCache( Document document ) : base( document )
    {
    }

    protected override SetupPrintStorable CreateNewStorable( Document document, string name ) => new SetupPrintStorable( document ) ;
  }
}