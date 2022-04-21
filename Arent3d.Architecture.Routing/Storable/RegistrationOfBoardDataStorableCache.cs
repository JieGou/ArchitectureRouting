using Arent3d.Revit ;
using Autodesk.Revit.Creation ;
using Document = Autodesk.Revit.DB.Document ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class RegistrationOfBoardDataStorableCache : StorableCache<RegistrationOfBoardDataStorableCache, RegistrationOfBoardDataStorable>
  {
    public RegistrationOfBoardDataStorableCache( Document document ) : base( document )
    {
    }

    protected override RegistrationOfBoardDataStorable CreateNewStorable( Document document, string name ) => new RegistrationOfBoardDataStorable( document ) ;
  }
}