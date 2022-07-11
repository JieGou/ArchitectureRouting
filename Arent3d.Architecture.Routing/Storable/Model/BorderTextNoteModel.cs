using System.Runtime.InteropServices ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  [Guid("CF1DB4C1-71AF-4C23-B382-4CD8008D149C")]
  public class BorderTextNoteModel
  {
    public string BorderIds { get ; set ; }

    public BorderTextNoteModel()
    {
      BorderIds = string.Empty ;
    }
    
    public BorderTextNoteModel(string? borderIds = default)
    {
      BorderIds = borderIds ?? string.Empty ;
    }
  }
}