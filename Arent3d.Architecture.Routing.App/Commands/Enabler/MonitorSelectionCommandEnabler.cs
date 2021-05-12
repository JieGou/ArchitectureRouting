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
    private ElementId? _previousSelectedRouteElementId = null ;

    public bool IsCommandAvailable( UIApplication uiApp, CategorySet selectedCategories )
    {
      var uiDoc = uiApp.ActiveUIDocument ;

      //If no Doc
      if ( uiDoc == null ) {
        return false ;
      }

      // Raise the SelectionChangedEvent
      var selectedRoutes = PointOnRoutePicker.PickedRoutesFromSelections( uiDoc ).EnumerateAll() ;
      
      ElementId? selectedElementId = null ;


      // bool iSelectMuliteElement = false;
      List<string> routeNameLst = new List<string>();
      ICollection<ElementId> elementIds = uiDoc.Selection.GetElementIds();

        foreach ( ElementId eid in elementIds ) {

            Element elem = uiDoc.Document.GetElement( eid );

            ParameterSet parameters = elem.Parameters;

            foreach ( Parameter param in parameters ) {
                if ( param.Definition.Name.Equals( "Route Name" ) ) {
                    if( routeNameLst.Contains( param.AsString() ) == false ) {
                        routeNameLst.Add( param.AsString() );
                    }
                }
            }

        }

        if( routeNameLst.Count > 1 ) {
                FromToTreeViewModel.ClearSelection();
                _previousSelectedRouteElementId = null;
         }

      // if route selected
      if ( selectedRoutes.FirstOrDefault() is {} selectedRoute ) {
        selectedElementId = selectedRoute.OwnerElement?.Id ;
        if ( selectedElementId != _previousSelectedRouteElementId ) {
          FromToTreeViewModel.GetSelectedElementId( selectedElementId ) ;
        }
        _previousSelectedRouteElementId = selectedElementId ; ;
      }
      
      // if Connector selected
      else if ( uiDoc.Document.CollectRoutes().SelectMany( r => r.GetAllConnectors() ).Any( c => uiDoc.Selection.GetElementIds().Contains( c.Owner.Id ) ) ) {
        selectedElementId = uiDoc.Selection.GetElementIds().FirstOrDefault() ;
        FromToTreeViewModel.GetSelectedElementId( selectedElementId ) ;
        _previousSelectedRouteElementId = selectedElementId ;
      }

      else if ( _previousSelectedRouteElementId != null ) {
        FromToTreeViewModel.ClearSelection() ;
        _previousSelectedRouteElementId = null ;
      }


      return false ;
    }
  }
}