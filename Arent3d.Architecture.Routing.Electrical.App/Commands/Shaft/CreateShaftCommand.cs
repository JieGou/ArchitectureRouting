using Arent3d.Architecture.Routing.AppBase.Commands.Shaft;
using Arent3d.Architecture.Routing.AppBase;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.EndPoints;
using Arent3d.Revit;
using Arent3d.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using ImageType = Arent3d.Revit.UI.ImageType;
namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Shaft
{
    [Transaction(TransactionMode.Manual)]
    [DisplayNameKey("Electrical.App.Commands.Shaft.CreateShaftCommand", DefaultString = "Create\nShaft")]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    public class CreateShaftCommand : CreateShaftCommandBase
    {
        protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickRouting";

        protected override AddInType GetAddInType()
        {
            return AddInType.Electrical;
        }

        protected override RoutingExecutor.CreateRouteGenerator GetRouteGeneratorInstantiator()
        {
            return RoutingApp.GetRouteGeneratorInstantiator();
        }

        protected override SetRouteProperty? CreateSegmentDialogWithConnector(Document document, Connector connector, MEPSystemClassificationInfo classificationInfo, IEndPoint fromEndPoint, IEndPoint toEndPoint)
        {
            var curveType = RouteMEPSystem.GetMEPCurveType(document, new[] { connector }, null);

            var diameter = fromEndPoint.GetDiameter() ?? toEndPoint.GetDiameter() ?? 0;

            return SetDialog(document, classificationInfo, RouteMEPSystem.GetSystemType(document, connector), curveType, diameter, fromEndPoint.RoutingStartPosition.Z.RevitUnitsToMillimeters());
        }

        protected override string GetNameBase(MEPSystemType? systemType, MEPCurveType curveType) => curveType.Category.Name;

        protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType(MEPSystemType? systemType)
        {
            return MEPSystemClassificationInfo.CableTrayConduit;
        }
    }
}
