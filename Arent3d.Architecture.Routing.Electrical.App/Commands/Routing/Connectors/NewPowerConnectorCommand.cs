using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing.Connectors ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing.Connectors
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewPowerConnectorCommand", DefaultString = "New Power Connector" )]
  [Image( "resources/new_connector_02.png" )]
  public class NewPowerConnectorCommand : NewConnectorCommandBase
  {
    protected override ElectricalRoutingFamilyType ElectricalRoutingFamilyType => ElectricalRoutingFamilyType.ConnectorOneSide ;
    protected override ConnectorFamilyType? ConnectorType => ConnectorFamilyType.Power ;
  }
}