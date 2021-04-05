using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Enabler
{
  public class MonitorSelectionCommandEnabler : IExternalCommandAvailability
  {
    public bool IsCommandAvailable( UIApplication uiApp, CategorySet selectedCategories )
    {
      var uiDoc = uiApp.ActiveUIDocument ;

      //If no Doc
      if ( uiDoc == null ) {
        return false ;
      }

      // Raise the SelectionChangedEvent
      List<ElementId> elementIds = uiApp.ActiveUIDocument.Selection.GetElementIds().OrderBy( id => id.IntegerValue ).ToList() ;

      TaskDialog.Show( "Selected Element", elementIds.ToString() ) ;

      return false ;
    }
  }
}