using Arent3d.Architecture.Routing.AppBase.Forms ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class LoadCsvFilesCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var dialog = new CsvModelDialog( commandData.Application ) ;

      dialog.ShowDialog() ;
      if ( dialog.DialogResult ?? false ) {
        UpdateCeedDockPaneDataContext( commandData.Application.ActiveUIDocument ) ;
        return Result.Succeeded ;
      }
      else {
        return Result.Cancelled ;
      }
    }
    
    protected virtual void UpdateCeedDockPaneDataContext( UIDocument uiDocument ) {}
  }
}