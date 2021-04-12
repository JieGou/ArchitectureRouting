using System.Collections.Generic ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.ViewModel
{
  public class FromToTreeViewModel : ViewModelBase
  {
    public void test()
    {
      TaskDialog.Show( "TEST", "test" ) ;
    }

    public IEnumerable<Route> GetAllRoutes( UIDocument uiDocument )
    {
      var allRoutes = uiDocument.Document.CollectRoutes() ;

      return allRoutes ;
    }
  }
}