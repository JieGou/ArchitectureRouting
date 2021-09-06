using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
    [Transaction(TransactionMode.Manual)]
    [DisplayNameKey("Electrical.App.Commands.Routing.NewDamperActuatorCommand", DefaultString = "ダンパ操作器")]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    public class NewDamperActuatorCommand : NewConnectorCommandBase
    {
        protected override RoutingFamilyType RoutingFamilyType => RoutingFamilyType.DamperActuator;
    }
}