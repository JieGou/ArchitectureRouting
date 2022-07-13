using Arent3d.Architecture.Routing.ExtensibleStorages ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  [Schema("CF2DB4C1-71AF-4C23-B382-4CD8008D149C", nameof(BorderTextNoteModel))]
  public class BorderTextNoteModel : IDataModel
  {
    [Field( Documentation = "The border of the text note." )]
    public string BorderUniqueIds { get ; set ; } = string.Empty ;
  }
}