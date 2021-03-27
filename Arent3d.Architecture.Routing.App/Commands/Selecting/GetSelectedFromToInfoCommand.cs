using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;
using Arent3d.Architecture.Routing.App.ViewModel ;


namespace Arent3d.Architecture.Routing.App.Commands.Selecting
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Modify From-To" )]
  [DisplayNameKey( "App.Commands.Selecting.GetSelectedFromToInfoCommand", DefaultString = "Modify From-To" )]
  [Image( "resources/MEP.ico" )]
  public class GetSelectedFromToInfoCommand : Routing.RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Selecting.GetSelectedFromToInfo" ;

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsBeforeTransaction( UIDocument uiDocument )
    {
      return ShowSelectedFromToDialog( uiDocument ) ;
    }

    private IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? ShowSelectedFromToDialog( UIDocument uiDocument )
    {
      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndReRoute.Pick".GetAppStringByKeyOrDefault( null ) ) ;

      RouteMEPSystem routeMepSystem = new RouteMEPSystem( uiDocument.Document, pickInfo.Route ) ;

      //Diameter Info
      var diameters = routeMepSystem.GetNominalDiameters( routeMepSystem.CurveType ).ToList() ;
      var diameter = pickInfo.SubRoute.GetDiameter( uiDocument.Document ) ;
      var diameterIndex = diameters.FindIndex( i => Math.Abs( i - diameter ) < uiDocument.Document.Application.VertexTolerance ) ;

      //System Type Info(PinpingSystemType in lookup)
      var connector = pickInfo.ReferenceConnector ;
      var systemTypes = routeMepSystem.GetSystemTypes( uiDocument.Document, connector ).OrderBy( s => s.Name ).ToList() ;
      var systemType = routeMepSystem.MEPSystemType ;
      var systemTypeIndex = systemTypes.FindIndex( s => s.Id == systemType.Id ) ;
      //CurveType Info
      var curveType = routeMepSystem.CurveType ;
      var type = curveType.GetType() ;
      var curveTypes = routeMepSystem.GetCurveTypes( uiDocument.Document, type ).OrderBy( s => s.Name ).ToList() ;
      var curveTypeIndex = curveTypes.FindIndex( c => c.Id == curveType.Id ) ;
      //Direct Info
      var direct = pickInfo.SubRoute.IsRoutingOnPipeSpace ;

      //Show Dialog with pickInfo
      SelectedFromToViewModel.ShowSelectedFromToDialog( uiDocument, diameterIndex, diameters, systemTypeIndex, systemTypes, curveTypeIndex, curveTypes, type, direct, pickInfo ) ;


      return AsyncEnumerable.Empty<(string RouteName, RouteSegment Segment)>() ;
    }
  }
}