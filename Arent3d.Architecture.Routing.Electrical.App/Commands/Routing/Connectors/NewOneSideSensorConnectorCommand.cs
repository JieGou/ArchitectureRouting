using Arent3d.Architecture.Routing.AppBase.Commands.Routing.Connectors ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing.Connectors
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewOneSideSensorConnectorCommand", DefaultString = "New One Side Sensor Connector" )]
  [Image( "resources/new_connector_02.png" )]
  public class NewOneSideSensorConnectorCommand : NewConnectorCommandBase
  {
    protected override ElectricalRoutingFamilyType ElectricalRoutingFamilyType => ElectricalRoutingFamilyType.ConnectorOneSide ;
    protected override ConnectorFamilyType? ConnectorType => ConnectorFamilyType.Sensor ;
  }
}