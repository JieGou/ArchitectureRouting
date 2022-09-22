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
    protected override IEnumerable<Element> GetRacks( UIDocument uiDocument)
    {
      var allLimitRack = GetAllLimitRackInstances( uiDocument.Document ) ;
      return allLimitRack ;
    }
  }
}