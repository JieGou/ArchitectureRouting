using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.Commands.Routing ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.ViewModel
{
  public class FromToTreeViewModel : ViewModelBase
  {
    
    public void test()
    {
      TaskDialog.Show( "TEST", "test" ) ;
    }

    public IEnumerable<Route> GetAllRoutes ( UIDocument uiDocument )
    {
      var allRoutes   =  uiDocument.Document.CollectRoutes() ;

      return allRoutes ;
    }
    
  }
}