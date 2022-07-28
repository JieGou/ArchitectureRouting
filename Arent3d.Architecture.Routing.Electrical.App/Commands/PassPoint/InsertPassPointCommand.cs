using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.PassPoint ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.PassPoint
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.PassPoint.InsertPassPointCommand", DefaultString = "Insert\nPass Point" )]
  [Image( "resources/InsertPassPoint.png", ImageType = ImageType.Large )]
  public class InsertPassPointCommand : InsertPassPointCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.PassPoint.Insert" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;
    
    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, PointOnRoutePicker.PickInfo pickInfo )
    {
      if ( ! pickInfo.RouteNameDictionary.Any() ) return ;
      RouteGenerator.ChangeRepresentativeRouteName( document, pickInfo.RouteNameDictionary ) ;
    }
  }
}