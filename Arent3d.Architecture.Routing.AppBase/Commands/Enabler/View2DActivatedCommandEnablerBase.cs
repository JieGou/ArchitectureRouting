using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Enabler
{
  public abstract class View2DActivatedCommandEnablerBase : IExternalCommandAvailability
  {
    protected abstract AddInType GetAddInType() ;

    public bool IsCommandAvailable( UIApplication uiApp, CategorySet selectedCategories )
    {
      var doc = uiApp.ActiveUIDocument?.Document ;
      return doc?.ActiveView is ViewPlan ;
    }
  }
}