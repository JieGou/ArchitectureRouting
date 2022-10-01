using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storages.Models
{
  [Schema( "22AFC5FC-BE44-4E2D-B60A-DFACBB0395AB", nameof( CategoryShowModel ) )]
  public class CategoryShowModel : IDataModel
  {
    [Field( Documentation = "Category List" )]
    public List<ElementId> CategoryIds { get ; set ; } = new List<ElementId>() ;
  }
}