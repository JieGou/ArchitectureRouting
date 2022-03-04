using Arent3d.Architecture.Routing.Mechanical.haseko.App.Forms ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  public class ReplaceFlexDuctCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      var view = new ReplaceFlexDuctView() ;
      
      return Result.Succeeded ;
    }
  }
}