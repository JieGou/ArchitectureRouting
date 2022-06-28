using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.ChangePullBoxDimensionCommand",
    DefaultString = "Change Pullbox Dimension" )]
  [Image( "resources/PickFrom-To.png" )]
  public class ChangePullBoxDimensionCommand : ChangePullBoxDimensionCommandBase
  {
    protected ElectricalRoutingFamilyType ElectricalRoutingFamilyType => ElectricalRoutingFamilyType.PullBox ;

    protected ConnectorFamilyType? ConnectorType => ConnectorFamilyType.PullBox ;

    protected override AddInType GetAddInType()
    {
      return AppCommandSettings.AddInType ;
    }
  }
}