using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.ShowFrom_ToWindowCommand", DefaultString = "From-To\nWindow" )]
  [Image( "resources/From-ToWindow.png" )]
  public class ShowFrom_ToWindowCommand : ShowFrom_ToWindowCommandBase
  {
    protected override AddInType GetAddInType()
    {
      return AddInType.Mechanical ;
    }
  }
}