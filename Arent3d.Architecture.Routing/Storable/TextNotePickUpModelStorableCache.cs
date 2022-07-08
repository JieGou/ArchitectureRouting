using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable
{
  public class TextNotePickUpModelStorableCache: StorableCache<TextNotePickUpModelStorableCache, TextNotePickUpModelStorable>
  {
    public TextNotePickUpModelStorableCache( Document document ) : base( document )
    {
      
    }
  
    protected override TextNotePickUpModelStorable CreateNewStorable( Document document, string name ) => new TextNotePickUpModelStorable( document ) ;
  }
}