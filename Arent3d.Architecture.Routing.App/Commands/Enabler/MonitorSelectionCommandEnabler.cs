using System.Collections.Generic ;
using System.Linq ;
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
      var selectedRoutes = PointOnRoutePicker.PickedRoutesFromSelections( uiDoc ).EnumerateAll() ;

      if ( 0 < selectedRoutes.Count ) {
        var selectedRouteName = selectedRoutes.ToList()[ 0 ].RouteName ;
        if ( selectedRouteName != PreviousSelectedRoute ) {
          TaskDialog.Show( "Selected Element From Enabler", selectedRouteName ) ;
        }
        PreviousSelectedRoute = selectedRoutes.ToList()[ 0 ].RouteName ;
      }
      

      return false ;
    }
  }
}