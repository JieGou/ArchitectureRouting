using System;
using System.Collections.Generic;
using System.ComponentModel ;
using System.Diagnostics;
using System.Linq;
using Arent3d.Architecture.Routing;
using Arent3d.Architecture.Routing.App.Forms;
using Arent3d.Revit.I18n;
using Arent3d.Revit.UI ;
using Arent3d.Utility;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI ;
using System.Collections.ObjectModel ;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using Arent3d.Architecture.Routing.App.ViewModel;
using Autodesk.Revit.DB.Plumbing;
using ICSharpCode.SharpDevelop;

namespace Arent3d.Architecture.Routing.App.Commands.Selecting
{
    [Transaction( TransactionMode.Manual )]
    [DisplayName( "Modify Selected From-To" )]
    public class GetSelectedFromToInfo : Routing.RoutingCommandBase
    {
        protected override string GetTransactionNameKey() => "TransactionName.Commands.Selecting.GetSelectedFromToInfo" ;

        protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsBeforeTransaction(UIDocument uiDocument)
        {
            var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
            
            if ( 0 < list.Count )
            {
                return ShowSelectedFromToDialog(uiDocument);
            }
            else 
            {
                return ShowSelectedFromToDialog(uiDocument);
            }
        }
        
        private IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? ShowSelectedFromToDialog(UIDocument uiDocument)
        {
            var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndReRoute.Pick".GetAppStringByKeyOrDefault( null ) ) ;
            
            RouteMEPSystem routeMepSystem = new RouteMEPSystem(uiDocument.Document, pickInfo.Route);

            //Diameter Info
            var diameterList = routeMepSystem.GetNominalDiameterList(routeMepSystem.CurveType);
            var diameter = pickInfo.SubRoute.GetDiameter(uiDocument.Document);
            var diameterIndex = diameterList.ToList().FindIndex(i => Math.Abs(i - diameter) <  uiDocument.Document.Application.VertexTolerance);
            
            //System Type Info

            /*var systemTypeList = pickInfo.ReferenceConnector.PipeSystemType;
            TaskDialog.Show("system type", RouteMEPSystem.GetSystemType(uiDocument.Document, pickInfo.ReferenceConnector).ToString());
            */

            //Direct Info
            var direct = pickInfo.SubRoute.IsRoutingOnPipeSpace;

            //Show Dialog with pickInfo
            SelectedFromToViewModel.ShowSelectedFromToDialog(uiDocument, diameterIndex, diameterList, direct, pickInfo);
            

            return AsyncEnumerable.Empty<(string RouteName, RouteSegment Segment)>();
        }
    }
}