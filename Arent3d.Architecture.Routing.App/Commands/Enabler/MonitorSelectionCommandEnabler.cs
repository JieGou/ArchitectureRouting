using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Enabler
{
  public class MonitorSelectionCommandEnabler : IExternalCommandAvailability
  {
    private string? PreviousSelectedRoute = null ;
    public bool IsCommandAvailable( UIApplication uiApp, CategorySet selectedCategories )
    {
      var uiDoc = uiApp.ActiveUIDocument ;

      //If no Doc
      if ( uiDoc == null ) {
        return false ;
      }

      // Raise the SelectionChangedEvent
      List<ElementId> elementIds = uiApp.ActiveUIDocument.Selection.GetElementIds().OrderBy( id => id.IntegerValue ).ToList() ;

      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDoc ).EnumerateAll() ;

      if ( 0 < list.Count ) {
        var selectedRouteName = list.ToList()[ 0 ].RouteName ;
        if ( selectedRouteName != PreviousSelectedRoute ) {
          FromToTreeViewModel.GetSelectedRouteName(selectedRouteName);
        }
        PreviousSelectedRoute = list.ToList()[ 0 ].RouteName ;
      }
      

      return false ;
    }
  }
}