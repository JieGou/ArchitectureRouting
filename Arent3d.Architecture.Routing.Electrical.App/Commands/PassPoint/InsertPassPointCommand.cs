using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.PassPoint ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.PassPoint
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.PassPoint.InsertPassPointCommand", DefaultString = "Insert\nPass Point" )]
  [Image( "resources/InsertPassPoint.png", ImageType = ImageType.Large )]
  public class InsertPassPointCommand : InsertPassPointCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.PassPoint.Insert" ;

    protected override AddInType GetAddInType()
    {
      return AddInType.Electrical ;
    }

    protected override RoutingExecutor.CreateRouteGenerator GetRouteGeneratorInstantiator()
    {
      return RoutingApp.GetRouteGeneratorInstantiator() ;
    }
  }
}