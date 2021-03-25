using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
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
            
            if (diameters != null && pickInfo != null && systemTypes != null && curveTypes != null)
            {
                //Change Diameter
                pickInfo.SubRoute.ChangePreferredNominalDiameter(diameters[SelectedFromToViewModel.SelectedDiameterIndex]);
                
                //Change SystemType
                pickInfo.SubRoute.ChangeSystemType(systemTypes[SelectedFromToViewModel.SelectedSystemTypeIndex]);
                RouteMEPSystem routeMepSystem = new RouteMEPSystem(uiDocument.Document, pickInfo.Route);
                
                //Change CurveType
                pickInfo.SubRoute.ChangeCurveType(curveTypes[SelectedFromToViewModel.SelectedCurveTypeIndex]);
                
                //Change Direct
                pickInfo.SubRoute.ChangeIsRoutingOnPipeSpace(SelectedFromToViewModel.IsDirect);

                //return base.GetRouteSegmentsInTransaction(uiDocument);
                // question IsDirect is false after Reroute
                return pickInfo.Route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
            }
            else
            {
                return base.GetRouteSegmentsInTransaction(uiDocument);
            }
            
        }
    }
}