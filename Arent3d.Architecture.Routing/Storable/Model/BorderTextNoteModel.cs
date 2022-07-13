using System.Collections.Generic ;
using Arent3d.Architecture.Routing.ExtensibleStorages ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  [Schema("CF4DB4C1-71AF-4C23-B382-5CD8008D149C", nameof(BorderTextNoteModel))]
  public class BorderTextNoteModel : IDataModel
  {
    [Field(Documentation = "TextNote")]
    public Dictionary<int, BorderModel> BorderTextNotes { get ; set ; } = new() ;
  }

  [Schema( "CF5DB4C1-71AF-4C23-B382-5CD8008D149C", nameof( BorderModel ) )]
  public class BorderModel : IDataModel
  {
    [Field(Documentation = "Border")]
    public List<ElementId> BorderIds { get ; set ; } = new() ;
  }
}