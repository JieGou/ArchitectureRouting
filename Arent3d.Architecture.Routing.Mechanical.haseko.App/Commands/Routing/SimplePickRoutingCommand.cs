using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Mechanical.Haseko.App.Forms ;
using Arent3d.Architecture.Routing.Mechanical.Haseko.App.Utils ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.Haseko.App.Commands.Routing.SimplePickRoutingCommand", DefaultString = "Simple\nPick From-To" )]
  [Image( "resources/PickFrom-To.png" )]
  public class SimplePickRoutingCommand : PickRoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickRouting" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override IDisposable? BeforeCommand( ExternalCommandData commandData, ElementSet elements )
    {
      base.BeforeCommand( commandData, elements ) ;
      return commandData ;
    }

    protected override void AfterCommand( IDisposable? commandSpanResource )
    {
      var result = MessageBox.Show( "PriorityBoxを表示しますか？", "通知", MessageBoxButton.OKCancel, MessageBoxImage.Information ) ;
      if ( result == MessageBoxResult.OK ) {
        if ( commandSpanResource is ExternalCommandData commandData ) {
          var document = commandData.Application.ActiveUIDocument.Document ;

          using var transaction = new Transaction( document, "TransactionName.Commands.Routing.CreatePriorityBox".GetAppStringByKeyOrDefault( "Create PriorityBox" ) ) ;
          transaction.Start() ;
          var collector = new FilteredElementCollector( document ) ;
          ElementCategoryFilter filterGen = new(BuiltInCategory.OST_GenericModel) ;
          const string envelopeParameter = "Obstacle Name" ;
          const string roomValueParam = "ROOM_BOX" ;

          var allRoomBox = collector
            .WherePasses( filterGen )
            .WhereElementIsNotElementType()
            .Where( r => r.LookupParameter( envelopeParameter ).AsString() == roomValueParam )
            .Select( x => x.Id )
            .ToList();

          if ( allRoomBox.Any() ) document.Delete( allRoomBox ) ;
          ObstacleGeneration.ShowRoomBox( document ) ;
          transaction.Commit() ;
        }
      }

      base.AfterCommand( commandSpanResource ) ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments
      ) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult,
        ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty,
        MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom )
    {
      return ( PickCommandUtil.CreateRouteEndPoint( newPickResult ), null ) ;
    }

    protected override DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      if ( RouteMEPSystem.GetSystemType( document, connector ) is not { } defaultSystemType ) return null ;

      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, defaultSystemType ) ;

      return new DialogInitValues( classificationInfo, defaultSystemType, curveType, connector.GetDiameter() ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => systemType?.Name ?? curveType.Category.Name ;

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
    {
      if ( null == systemType ) return null ;
      return MEPSystemClassificationInfo.From( systemType! ) ;
    }

    protected override IRoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, initValues.ClassificationInfo, fromLevelId, toLevelId ) ;
      SimplePickRoutingUtil.SetFromHeightLevelSetting( document, fromLevelId, toLevelId, ref routeChoiceSpec ) ;
      var routeProperty = new RouteProperties( document, initValues.SystemType, initValues.CurveType, initValues.Diameter, false, false, null, false, null, AvoidType.Whichever, null ) ;
      var sv = new SimpleRoutePropertyDialog( document, routeChoiceSpec, routeProperty ) ;

      sv.ShowDialog() ;

      return sv ;
    }
  }
}