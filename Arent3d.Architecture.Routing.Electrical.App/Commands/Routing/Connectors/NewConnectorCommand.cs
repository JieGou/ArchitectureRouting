using Arent3d.Architecture.Routing.AppBase.Commands.Routing.Connectors ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing.Connectors
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewConnectorCommand", DefaultString = "New Connector" )]
  [Image( "resources/new_connector_02.png" )]
  public class NewConnectorCommand : NewConnectorCommandBase
  {
    protected override RoutingFamilyType RoutingFamilyType => RoutingFamilyType.ConnectorOneSide ;
  }
}