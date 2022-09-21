using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseAllRackCommandBase : EraseRackCommandBase
  {
    protected override IEnumerable<string> GetLimitRackUniqueIds( UIDocument uiDocument, Document document )
    {
      var allLimitRack = GetAllLimitRackInstances( document ) ;
      var allLimitRackIds = allLimitRack.Select( x => x.UniqueId ).EnumerateAll() ;
      return allLimitRackIds ;
    }
  }
}