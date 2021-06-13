using System ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ShowFromToTreeCommandBase : IExternalCommand
  {
    protected UIDocument? _uiDocument = null ;

    public abstract Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements ) ;
  }
}