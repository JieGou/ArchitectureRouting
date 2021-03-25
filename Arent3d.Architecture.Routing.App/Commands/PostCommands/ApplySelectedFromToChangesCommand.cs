using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Arent3d.Architecture.Routing.App.ViewModel;
using Arent3d.Revit ;
using Arent3d.Utility;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.PostCommands
{
    [RevitAddin( Guid )]
    [DisplayName( "Apply Selected From-To Changes" )]
    [Transaction( TransactionMode.Manual )]
    public class ApplySelectedFromToChangesCommand : Routing.RoutingCommandBase
    {
        private const string Guid = "D1464970-1251-442F-8754-E59E293FBC9D";
        protected override string GetTransactionNameKey() => "TransactionName.Commands.PostCommands.ApplySelectedFromToChangesCommand" ;
        
        /*public ApplySelectedFromToChangesCommand(PointOnRoutePicker.PickInfo pickInfo, int diameterIndex)
        {
            TargetPickInfo = pickInfo;
            SelectedIndex = diameterIndex;
        }*/
        
        protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsBeforeTransaction(UIDocument uiDocument)
        {
            var pickInfo = SelectedFromToViewModel.TargetPickInfo;
            var diameters = SelectedFromToViewModel.Diameters;
            var systemTypes = SelectedFromToViewModel.SystemTypes;
            var curveTypes = SelectedFromToViewModel.CurveTypes;
            
            if (diameters != null && pickInfo != null)
            {
                //Change Diameter
                pickInfo.SubRoute.ChangePreferredNominalDiameter(diameters[SelectedFromToViewModel.SelectedDiameterIndex]);
                
                //Change SystemType
                RouteMEPSystem routeMepSystem = new RouteMEPSystem(uiDocument.Document, pickInfo.Route);
                //routeMepSystem.MEPSystem = systemTypeList[SelectedFromToViewModel.SelectedSystemTypeIndex];
                
                //Change CurveType
                TaskDialog.Show("curvetype", routeMepSystem.CurveType.Name);
                //routeMepSystem.CurveType = curveTypes[SelectedFromToViewModel.SelectedCurveTypeIndex];
                
                //Change Direct
                pickInfo.SubRoute.ChangeIsRoutingOnPipeSpace(SelectedFromToViewModel.IsDirect);

                
                return pickInfo.Route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
            }
            else
            {
                return base.GetRouteSegmentsInTransaction(uiDocument);
            }
            
        }
    }
}