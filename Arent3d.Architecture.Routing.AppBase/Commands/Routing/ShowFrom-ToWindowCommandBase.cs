using System ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ShowFrom_ToWindowCommandBase : IExternalCommand
  {
    private UIDocument? _uiDocument = null ;
    protected abstract AddInType GetAddInType() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      _uiDocument = commandData.Application.ActiveUIDocument ;
      try {
        FromToWindowViewModel.ShowFromToWindow( _uiDocument, GetAddInType() ) ;
      }
      catch ( Exception e ) {
        TaskDialog.Show( "ShowFrom_ToWindowCommand", e.Message ) ;
      }

      return Result.Succeeded ;
    }
  }
}