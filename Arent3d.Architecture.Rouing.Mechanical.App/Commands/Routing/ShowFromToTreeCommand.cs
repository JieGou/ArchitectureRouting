using System ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  [DisplayNameKey( "App.Commands.Routing.ShowFromTreeCommand", DefaultString = "From-To\nTree" )]
  [Image( "resources/MEP.ico" )]
  public class ShowFromToTreeCommand : IExternalCommand
  {
    private UIDocument? _uiDocument = null ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      _uiDocument = commandData.Application.ActiveUIDocument ;

      try {
        var dpid = new DockablePaneId( PaneIdentifiers.GetFromToTreePaneIdentifier() ) ;
        var dp = _uiDocument.Application.GetDockablePane( dpid ) ;
        if ( ! dp.IsShown() ) {
          dp.Show() ;
        }
        else {
          dp.Hide() ;
        }
      }
      catch ( Exception e ) {
        TaskDialog.Show( "ShowFromToTreeCommand", e.Message ) ;
      }

      return Result.Succeeded ;
    }
  }
}