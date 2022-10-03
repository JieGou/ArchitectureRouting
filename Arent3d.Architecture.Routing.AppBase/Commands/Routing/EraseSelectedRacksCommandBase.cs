using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseSelectedRacksCommandBase : EraseRackCommandBase
  {
    protected override IEnumerable<Element> GetRacks( UIDocument uiDocument )
    {
      try {
        var selectedLimitRackRefElements = uiDocument.Selection.PickElementsByRectangle( RackReferenceSelectionFilter.Instance, "削除の範囲を指定してください。" ).EnumerateAll() ;
        return selectedLimitRackRefElements.Any() ? selectedLimitRackRefElements : new List<Element>() ;
      }
      catch ( OperationCanceledException ) {
        return Array.Empty<Element>() ;
      }
    }
  }
}