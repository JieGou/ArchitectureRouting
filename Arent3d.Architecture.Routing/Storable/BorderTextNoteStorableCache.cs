using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class BorderTextNoteStorableCache: StorableCache<BorderTextNoteStorableCache, BorderTextNoteStorable>
  {
    public BorderTextNoteStorableCache( Document document ) : base( document )
    {
      
    }
  
    protected override BorderTextNoteStorable CreateNewStorable( Document document, string name ) => new BorderTextNoteStorable( document ) ;
  }
}