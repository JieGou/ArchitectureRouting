using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  public class RegisterSymbolCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var registerSymbolView = new RegisterSymbolView
      {
        DataContext = new RegisterSymbolViewModel( commandData.Application.ActiveUIDocument )
      } ;
      registerSymbolView.ShowDialog() ;
      
      return Result.Succeeded ; 
    }
  }
}