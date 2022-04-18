using System.Collections.Generic ;
using System.Linq ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class BorderTextNoteModel
  {
    public int TextNoteId { get ; set ; }
    public string BorderIds { get ; set ; }
  
    public BorderTextNoteModel(int? textNoteId = default, string? borderIds = default)
    {
      TextNoteId = textNoteId ?? 0 ;
      BorderIds = borderIds ?? string.Empty ;
    }
  }
}