using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.PickRoutingCommand", DefaultString = "Pick\nFrom-To" )]
  [Image( "resources/PickFrom-To.png" )]
  public class PickRoutingCommand : PickRoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickRouting" ;

    protected override AddInType GetAddInType()
    {
      return AddInType.Mechanical ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult ) ;

      MEPSystemClassificationInfo? classificationInfo ;
      MEPSystemType? systemType ;
      MEPCurveType? curveType ;
      double? dblDiameter = 0 ;

      var list = new List<(string RouteName, RouteSegment Segment)>() ;
      var connector = fromEndPoint.GetReferenceConnector() ?? toEndPoint.GetReferenceConnector() ;

      if ( fromPickResult.SubRoute is { } subRoute1 ) {
        //Set property from Dialog
        classificationInfo = subRoute1.Route.GetSystemClassificationInfo() ;
        systemType = subRoute1.Route.GetMEPSystemType() ;
        curveType = subRoute1.Route.GetDefaultCurveType() ;
        dblDiameter = subRoute1.GetDiameter() ;
        var sv = SetDialog( document, classificationInfo, systemType, curveType, dblDiameter ) ;
        if ( false != sv.DialogResult ) {
          return CreateNewSegmentListForRoutePick( subRoute1, fromPickResult, toEndPoint, false, classificationInfo, sv ) ;
        }
        else {
          return null ;
        }
      }

      if ( toPickResult.SubRoute is { } subRoute2 ) {
        //Set property from Dialog
        classificationInfo = subRoute2.Route.GetSystemClassificationInfo() ;
        systemType = subRoute2.Route.GetMEPSystemType() ;
        curveType = subRoute2.Route.GetDefaultCurveType() ;
        dblDiameter = subRoute2.GetDiameter() ;

        var sv = SetDialog( document, classificationInfo, systemType, curveType, dblDiameter ) ;
        if ( false != sv.DialogResult ) {
          return CreateNewSegmentListForRoutePick( subRoute2, toPickResult, fromEndPoint, true, classificationInfo, sv ) ;
        }
        else {
          return null ;
        }
      }

      var routes = RouteCache.Get( document ) ;

      if ( connector != null ) {
        //Set property from Dialog
        classificationInfo = MEPSystemClassificationInfo.From( connector ) ;
        systemType = RouteMEPSystem.GetSystemType( document, connector ) ;
        if ( systemType is { } defaultSystemType ) {
          if ( classificationInfo is null ) return list ;

          curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, defaultSystemType ) ;

          if ( fromEndPoint.GetDiameter() is { } d1 ) {
            dblDiameter = d1 ;
          }
          else if ( toEndPoint.GetDiameter() is { } d2 ) {
            dblDiameter = d2 ;
          }

          var sv = SetDialog( document, classificationInfo, defaultSystemType, curveType, dblDiameter ) ;

          if ( false != sv.DialogResult ) {
            systemType = sv.GetSelectSystemType() ;
            curveType = sv.GetSelectCurveType() ;

            var nextIndex = GetRouteNameIndex( routes, systemType?.Name ) ;

            var name = systemType?.Name + "_" + nextIndex ;

            var isDirect = false ;
            if ( sv.GetCurrentDirect() is { } currentDirect ) {
              isDirect = currentDirect ;
            }

            double? targetFixedHeight = sv.GetFixedHeight() ;
            if ( targetFixedHeight is { } fixedBoxHeight ) {
              targetFixedHeight = fixedBoxHeight.MillimetersToRevitUnits() ;
            }

            var segment = new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, sv.GetSelectDiameter().MillimetersToRevitUnits(), isDirect, targetFixedHeight, sv.GetAvoidTypeKey() ) ;

            routes.FindOrCreate( name ) ;
            list.Add( ( name, segment ) ) ;

            return list ;
          }

          return null ;
        }
      }
      else {
        SetRouteProperty sv = new SetRouteProperty() ;
        PropertySource.RoutePropertySource PropertySourceType = new PropertySource.RoutePropertySource( document ) ;
        SelectedFromToViewModel.PropertySourceType = PropertySourceType ;
        sv.UpdateFromToParameters( PropertySourceType.Diameters, PropertySourceType.SystemTypes, PropertySourceType.CurveTypes, PropertySourceType.SystemType!, PropertySourceType.CurveType!, dblDiameter ) ;

        sv.ShowDialog() ;

        if ( false != sv.DialogResult ) {
          systemType = sv.GetSelectSystemType() ;
          curveType = sv.GetSelectCurveType() ;

          var nextIndex = GetRouteNameIndex( routes, systemType?.Name ) ;

          var name = systemType?.Name + "_" + nextIndex ;
          var isDirect = false ;
          if ( sv.GetCurrentDirect() is { } currentDirect ) {
            isDirect = currentDirect ;
          }

          double? targetFixedHeight = sv.GetFixedHeight() ;
          if ( targetFixedHeight is { } fixedBoxHeight ) {
            targetFixedHeight = fixedBoxHeight.MillimetersToRevitUnits() ;
          }

          classificationInfo = MEPSystemClassificationInfo.From( systemType! ) ;
          var segment = new RouteSegment( classificationInfo!, systemType, curveType, fromEndPoint, toEndPoint, sv.GetSelectDiameter().MillimetersToRevitUnits(), isDirect, targetFixedHeight, sv.GetAvoidTypeKey() ) ;
          routes.FindOrCreate( name ) ;
          list.Add( ( name, segment ) ) ;

          return list ;
        }

        return null ;
      }

      return null ;
    }
  }
}