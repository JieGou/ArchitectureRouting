using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [Transaction( TransactionMode.ReadOnly )]
  public class TestCommand : IExternalCommand
  {
    private const string Guid = "30dbe347-2685-4f4b-aedb-0d1c34e255d5" ;
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      TaskDialog.Show( "Test", "Command!" ) ;
      return Result.Succeeded ;
    }
  }
}