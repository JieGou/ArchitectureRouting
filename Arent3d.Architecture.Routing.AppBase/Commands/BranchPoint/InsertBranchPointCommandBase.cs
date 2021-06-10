using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.BranchPoint
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.BranchPoint.InsertBranchPointCommand", DefaultString = "Insert\nBranch Point" )]
  [Image( "resources/InsertBranchPoint.png", ImageType = ImageType.Large )]
  public abstract class InsertBranchPointCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var dialog = new ShowDialog( this.ToString() ) ;
      dialog.Show() ;

      return Result.Succeeded ;
    }
  }
}