using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
    public class ShaftOpeningStorableCache : StorableCache<ShaftOpeningStorableCache, ShaftOpeningStorable>
    {
        public ShaftOpeningStorableCache( Document document ) : base( document )
        {
            
        }

        protected override ShaftOpeningStorable CreateNewStorable( Document document, string name ) => new ( document ) ;
    }
}