using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.ShowFromTreeCommand", DefaultString = "From-To\nTree" )]
  [Image( "resources/MEP.ico" )]
  public class ShowFromToTreeCommand : ShowFromToTreeCommandBase
  {
  }
}