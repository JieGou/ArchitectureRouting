using System.Collections.Generic ;
using System.Linq ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class BorderTextNoteModel
  {
    public string TextNoteUniqueId { get ; set ; }
    public string BorderUniqueIds { get ; set ; }
  
    public BorderTextNoteModel(string? textNoteUniqueId = default, string? borderUniqueIds = default)
    {
      TextNoteUniqueId = textNoteUniqueId ?? string.Empty ;
      BorderUniqueIds = borderUniqueIds ?? string.Empty ;
    }
  }
}