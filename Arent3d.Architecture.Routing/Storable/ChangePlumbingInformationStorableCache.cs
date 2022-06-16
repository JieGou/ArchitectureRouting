using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class ChangePlumbingInformationStorableCache: StorableCache<ChangePlumbingInformationStorableCache, ChangePlumbingInformationStorable>
  {
    public ChangePlumbingInformationStorableCache( Document document ) : base( document )
    {
    }

    protected override ChangePlumbingInformationStorable CreateNewStorable( Document document, string name ) => new( document ) ;
  }
}