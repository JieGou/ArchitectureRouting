using System ;
using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ShowFrom_ToWindowCommandBase : IExternalCommand
  {
    private UIDocument? _uiDocument = null ;
    protected abstract AddInType GetAddInType() ;

    protected abstract FromToWindowBase CreateFromToWindow( UIDocument uiDocument, ObservableCollection<FromToWindowBase.FromToItems> fromToItemsList ) ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      _uiDocument = commandData.Application.ActiveUIDocument ;
      try {
        FromToWindowViewModel.ShowFromToWindow( _uiDocument, GetAddInType(), CreateFromToWindow ) ;
      }
      catch ( Exception e ) {
        TaskDialog.Show( "ShowFrom_ToWindowCommand", e.Message ) ;
      }

      return Result.Succeeded ;
    }
  }
}