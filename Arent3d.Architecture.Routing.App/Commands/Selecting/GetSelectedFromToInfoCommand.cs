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
using Arent3d.Revit;
using Autodesk.Revit.DB.Plumbing;
using ICSharpCode.SharpDevelop;

namespace Arent3d.Architecture.Routing.App.Commands.Selecting
{
    [Transaction( TransactionMode.Manual )]
    [DisplayName( "Modify From-To" )]
    [DisplayNameKey( "App.Commands.Selecting.GetSelectedFromToInfoCommand", DefaultString = "Modify From-To" )]
    [Image( "resources/MEP.ico" )]
    public class GetSelectedFromToInfoCommand : Routing.RoutingCommandBase
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
            var diameters = routeMepSystem.GetNominalDiameters(routeMepSystem.CurveType);
            var diameter = pickInfo.SubRoute.GetDiameter(uiDocument.Document);
            var diameterIndex = diameters.ToList().FindIndex(i => Math.Abs(i - diameter) <  uiDocument.Document.Application.VertexTolerance);
            
            //System Type Info(PinpingSystemType in lookup)
            var connector = pickInfo.ReferenceConnector;
            var systemTypes = routeMepSystem.GetSystemTypes(uiDocument.Document, connector)
                .Select(s => new { Sorted = s, Index = s.Name}).OrderBy(s => s.Index).Select(s => s.Sorted).ToList();
            var systemType = routeMepSystem.MEPSystemType;
            var systemTypeIndex = systemTypes.Select(s => s.Id).ToList().FindIndex(n => n == systemType.Id);

            //CurveType Info
            var curveType = routeMepSystem.CurveType;
            var type = curveType.GetType();
            var curveTypes = routeMepSystem.GetCurveTypes(uiDocument.Document, type)
                .Select(c => new { Sorted = c, Index = c.Name}).OrderBy(c => c.Index).Select(c => c.Sorted).ToList();;
            var curveTypeIndex = curveTypes.Select(s => s.Id).ToList().FindIndex(n => n == curveType.Id);

            //Direct Info
            var direct = pickInfo.SubRoute.IsRoutingOnPipeSpace;
            
            //Show Dialog with pickInfo
            SelectedFromToViewModel.ShowSelectedFromToDialog(uiDocument, diameterIndex, diameters, systemTypeIndex, systemTypes 
                ,curveTypeIndex, curveTypes, type,  direct, pickInfo);
            

            return AsyncEnumerable.Empty<(string RouteName, RouteSegment Segment)>();
        }
    }
}