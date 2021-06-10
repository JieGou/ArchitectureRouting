using Arent3d.Architecture.Routing.AppBase.Commands.BranchPoint;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.BranchPoint
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.BranchPoint.InsertBranchPointCommand", DefaultString = "Insert\nBranch Point" )]
  [Image( "resources/InsertBranchPoint.png", ImageType = ImageType.Large )]
  public class InsertBranchPointCommand : InsertBranchPointCommandBase
  {
  }
  
}