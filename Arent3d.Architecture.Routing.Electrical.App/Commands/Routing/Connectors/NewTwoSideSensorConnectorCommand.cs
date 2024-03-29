﻿using Arent3d.Architecture.Routing.AppBase.Commands.Routing.Connectors ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing.Connectors
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewTwoSideConnectorTypeSensorCommand", DefaultString = "New Two Side Sensor Connector" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class NewTwoSideConnectorTypeSensorCommand : NewConnectorCommandBase
  {
    protected override ElectricalRoutingFamilyType ElectricalRoutingFamilyType => ElectricalRoutingFamilyType.ConnectorTwoSide ;
    protected override ConnectorFamilyType? ConnectorType => ConnectorFamilyType.Sensor ;
  }
}