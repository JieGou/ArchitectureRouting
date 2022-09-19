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
  public class EraseSelectedLimitRacksCommandBase : EraseLimitRackCommandBase
  {
    protected override IEnumerable<string> GetLimitRackUniqueIds( UIDocument uiDocument, Document document )
    {
      var selectedLimitRackRefElements = uiDocument.Selection
        .PickElementsByRectangle( LimitRackReferenceSelectionFilter.Instance, "Please select any rack or rack detail curve by mouse drag." ).EnumerateAll() ;
      return selectedLimitRackRefElements.Any() ? selectedLimitRackRefElements.Select( x => x.UniqueId ).ToList() : new List<string>() ;
    }
  }
}