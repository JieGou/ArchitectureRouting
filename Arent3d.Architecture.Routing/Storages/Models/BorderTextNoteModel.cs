using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Storages.Models
{
    [Schema("9BE0FD01-F9C9-4389-AC44-6AB71FBC2DFE", nameof( BorderTextNoteModel ))]
    public class BorderTextNoteModel : IDataModel
    {
        [Field(Documentation = "TextNote")]
        public Dictionary<int, BorderModel> BorderTextNotes { get ; set ; } = new() ;
    }

    [Schema( "2C78C119-F027-4DC2-8BE8-2A2D0EF42002", nameof( BorderModel ) )]
    public class BorderModel : IDataModel
    {
        [Field(Documentation = "Border")]
        public List<ElementId> BorderIds { get ; set ; } = new() ;
    }
}