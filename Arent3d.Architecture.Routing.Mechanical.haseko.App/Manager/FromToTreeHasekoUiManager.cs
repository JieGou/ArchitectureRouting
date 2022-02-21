using System ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Forms ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Manager
{
  public class FromToTreeHasekoUiManager : FromToTreeUiManager
  {
    public FromToTreeHaseko FromToTreeHasekoView { get ;  }
    public FromToTreeHasekoUiManager( UIControlledApplication uiControlledApplication, Guid dpId, string fromToTreeTitle, IPostCommandExecutorBase postCommandExecutor, FromToItemsUiBase fromToItemsUi )
    : base(uiControlledApplication, dpId, fromToTreeTitle, postCommandExecutor, fromToItemsUi)
    { 
      FromToTreeHasekoView = new FromToTreeHaseko( fromToTreeTitle, postCommandExecutor, fromToItemsUi ) ;
    }
  }
}