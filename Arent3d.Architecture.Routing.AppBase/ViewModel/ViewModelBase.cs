using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class ViewModelBase
  {
    protected static UIDocument? UiDoc { get ; set ; }

    protected static RevitDialog? OpenedDialog ;
  }
}