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
    private ElementId? PreviousSelectedRouteElementId = null ;

    public bool IsCommandAvailable( UIApplication uiApp, CategorySet selectedCategories )
    {
      var uiDoc = uiApp.ActiveUIDocument ;

      //If no Doc
      if ( uiDoc == null ) {
        return false ;
      }

      // Raise the SelectionChangedEvent
      var selectedRoutes = PointOnRoutePicker.PickedRoutesFromSelections( uiDoc ).EnumerateAll() ;
      var selectedConnectors = uiDoc.Document.CollectRoutes().SelectMany( r => r.GetAllConnectors() ).ToList() ;
      
      ElementId? selectedElementId = null ;
      // if route selected
      if ( selectedRoutes.FirstOrDefault() is {} selectedRoute ) {
        selectedElementId = selectedRoute.OwnerElement?.Id ;
        if ( selectedElementId != PreviousSelectedRouteElementId ) {
          FromToTreeViewModel.GetSelectedElementId( selectedElementId ) ;
        }
        PreviousSelectedRouteElementId = selectedElementId ; ;
      }
      
      // if Connector selected
      else if ( selectedConnectors.Any( c => uiDoc.Selection.GetElementIds().Contains(c.Owner.Id) )  ) {
        selectedElementId = uiDoc.Selection.GetElementIds().FirstOrDefault() ;
        FromToTreeViewModel.GetSelectedElementId( selectedElementId ) ;
        PreviousSelectedRouteElementId = selectedElementId ; 
      }
      else if ( PreviousSelectedRouteElementId != null ) {
        FromToTreeViewModel.ClearSelection() ;
        PreviousSelectedRouteElementId = null ;
      }
      


      return false ;
    }
  }
}