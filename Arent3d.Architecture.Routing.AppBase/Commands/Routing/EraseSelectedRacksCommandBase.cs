using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseSelectedRacksCommandBase : EraseRackCommandBase
  {
    protected override IEnumerable<Element> GetRacks( UIDocument uiDocument)
    {
      var selectedLimitRackRefElements = uiDocument.Selection
        .PickElementsByRectangle( LimitRackReferenceSelectionFilter.Instance, "Please select any rack or rack detail curve by mouse drag." ).EnumerateAll() ;
      return selectedLimitRackRefElements.Any() ? selectedLimitRackRefElements : new List<Element>() ;
    }
  }
}