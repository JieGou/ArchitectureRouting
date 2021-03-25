using System.Collections.Generic;
using System.ComponentModel;
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
            var diameterList = SelectedFromToViewModel.DiameterList;
            var systemTypeList = SelectedFromToViewModel.SystemTypeList;
            
            if (diameterList != null && pickInfo != null)
            {
                //Change Diameter
                pickInfo.SubRoute.ChangePreferredNominalDiameter(diameterList[SelectedFromToViewModel.SelectedDiameterIndex]);
                TaskDialog.Show( "Selected Diameter", UnitUtils.ConvertFromInternalUnits(pickInfo.SubRoute.GetDiameter(uiDocument.Document),UnitTypeId.Millimeters).ToString() ) ;
                
                //Change SystemType
                RouteMEPSystem routeMepSystem = new RouteMEPSystem(uiDocument.Document, pickInfo.Route);
                //routeMepSystem.MEPSystem = systemTypeList[SelectedFromToViewModel.SelectedSystemTypeIndex];
                
                //Change Direct
                pickInfo.SubRoute.ChangeIsRoutingOnPipeSpace(SelectedFromToViewModel.IsDirect);
                TaskDialog.Show("Direct", pickInfo.SubRoute.IsRoutingOnPipeSpace.ToString());
                
                return pickInfo.Route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
            }
            else
            {
                return base.GetRouteSegmentsInTransaction(uiDocument);
            }
            
        }
    }
}