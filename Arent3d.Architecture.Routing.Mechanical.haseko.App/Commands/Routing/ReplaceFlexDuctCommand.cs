using System ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Forms ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.ViewModel ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  public class ReplaceFlexDuctCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      try {
        
        
        
        
        
        var replaceFlexDuctView = new ReplaceFlexDuctView() { DataContext = new ReplaceFlexDuctViewModel(document) } ;
        replaceFlexDuctView.ShowDialog() ;
        
        

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException(exception);
        return Result.Failed;
      }
    }
  }
}