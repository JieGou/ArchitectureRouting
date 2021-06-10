using Arent3d.Architecture.Routing.AppBase.Commands.BranchPoint ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.BranchPoint
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.BranchPoint.InsertBranchPointCommand", DefaultString = "Insert\nBranch Point" )]
  [Image( "resources/InsertBranchPoint.png", ImageType = ImageType.Large )]
  public class InsertBranchPointCommand : InsertBranchPointCommandBase
  {
  }
}