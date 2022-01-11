using System ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  [DisplayNameKey( "Mechanical.haseko.App.Commands.Routing.ShowFromTreeCommand", DefaultString = "From-To\nTree" )]
  [Image( "resources/MEP.ico" )]
  public class ShowFromToTreeCommand : ShowFromToTreeCommandBase
  {
    public override Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      _uiDocument = commandData.Application.ActiveUIDocument ;

      try {
        var dpid = RoutingApp.FromToTreeManager.FromToTreeUiManager?.DpId ;
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