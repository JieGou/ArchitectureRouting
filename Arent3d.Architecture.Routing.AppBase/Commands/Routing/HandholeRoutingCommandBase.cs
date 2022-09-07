using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class HandholeRoutingCommandBase : RoutingCommandBase<HandholeRoutingCommandBase.PickState>
  {
    public record PickState( PointOnRoutePicker.PickInfo PickInfo, FamilyInstance Handhole, double HeightConnector, double HeightWire, XYZ RouteDirection, bool IsCreateHandholeWithoutSettingHeight, bool IsAutoCalculateHandholeSize,
      XYZ? PositionLabel, HandholeModel? SelectedHandhole, XYZ? FromDirection, XYZ? ToDirection, Dictionary<string, List<string>> ParentAndChildRoute ) ;

    private const string DefaultBuzaicdForGradeModeThanThree = "032025" ;
    private const string HandholeName = "ハンドホール" ;
    private const string NoFamily = "There is no handhole family in this project" ;
    private const string Error = "Error" ;
    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }
    protected virtual ConnectorFamilyType? ConnectorType => null ;
    protected abstract AddInType GetAddInType() ;
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      if ( ! document.GetFamilySymbols( ElectricalRoutingFamilyType ).Any() ) {
        MessageBox.Show( NoFamily, Error, MessageBoxButtons.OK, MessageBoxIcon.Error ) ;
        return OperationResult<PickState>.Cancelled ;
      }

      PointOnRoutePicker.PickInfo? pickInfo ;
      try {
        pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick point on Route", GetAddInType(), PointOnRouteFilters.RepresentativeElement ) ;
      }
      catch ( OperationCanceledException ) {
        return OperationResult<PickState>.Cancelled ;
      }

      var (originX, originY, originZ) = pickInfo.Position ;
      XYZ? fromDirection = null ;
      XYZ? toDirection = null ;
      if ( pickInfo.Element is FamilyInstance conduitFitting ) {
        var pullBoxInfo = PullBoxRouteManager.GetPullBoxInfo( document, pickInfo.Route.RouteName, conduitFitting ) ;
        ( originX, originY, originZ ) = pullBoxInfo.Position ;
        fromDirection = pullBoxInfo.FromDirection ;
        toDirection = pullBoxInfo.ToDirection ;
      }

      var level = ( document.GetElement( pickInfo.Element.GetLevelId() ) as Level ) ! ;
      var heightConnector = originZ - level.Elevation ;
      var heightWire = originZ - level.Elevation ;

      using Transaction transaction = new(document, "Create Handhole") ;
      transaction.Start() ;
      var handhole = PullBoxRouteManager.GenerateConnector( document, ElectricalRoutingFamilyType, ConnectorType, originX, originY, heightConnector, level, pickInfo.Route.Name ) ;
      transaction.Commit() ;

      XYZ? position ;
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;
      if ( pickInfo.Element is FamilyInstance { FacingOrientation: { } } ) {
        position = new XYZ( originX + 0.4 * baseLengthOfLine, originY + 0.7 * baseLengthOfLine, heightConnector ) ;
      }
      else if ( pickInfo.RouteDirection.X is 1.0 or -1.0 ) {
        position = new XYZ( originX, originY + 0.7 * baseLengthOfLine, heightConnector ) ;
      }
      else if ( pickInfo.RouteDirection.Y is 1.0 or -1.0 ) {
        position = new XYZ( originX + 0.4 * baseLengthOfLine, originY + 0.7 * baseLengthOfLine, heightConnector ) ;
      }
      else {
        position = new XYZ( originX, originY, heightConnector ) ;
      }

      HandholeModel? handholeModel = null ;
      if ( GetHandholeModels( document ) is { Count: > 0 } handholeModels ) {
        handholeModel = handholeModels.FirstOrDefault( model => model.Buzaicd == DefaultBuzaicdForGradeModeThanThree ) ?? handholeModels.First() ;
      }

      return new OperationResult<PickState>(
        new PickState( pickInfo, handhole, heightConnector, heightWire, pickInfo.RouteDirection, true, true, position, handholeModel, fromDirection, toDirection, new Dictionary<string, List<string>>() ) ) ;
    }

    private List<HandholeModel> GetHandholeModels( Document document )
    {
      var csvStorable = document.GetCsvStorable() ;
      var allHandholeHiroiMasterModel = csvStorable.HiroiMasterModelData.Where( hr => hr.Hinmei.Contains( HandholeName ) ) ;
      var handholeModels = allHandholeHiroiMasterModel.Select( hiroiMasterModel => new HandholeModel( hiroiMasterModel ) ).ToList() ;
      return handholeModels.OrderBy( pb => pb.SuffixCategoryName ).ThenBy( pb => pb.PrefixCategoryName ).ThenBy( pb => pb.Width ).ThenBy( pb => pb.Height ).ToList() ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      var (pickInfo, pullBox, heightConnector, heightWire, routeDirection, isCreateHandholeWithoutSettingHeight, _, _, _, fromDirection, toDirection, parentAndChildRoute) = pickState ;
      var route = pickInfo.SubRoute.Route ;
      var systemType = route.GetMEPSystemType() ;
      var curveType = route.UniqueCurveType ;
      var nameBase = GetNameBase( systemType, curveType! ) ;
      var parentIndex = 1 ;
      var allowedTiltedPiping = CheckAllowedTiltedPiping( route.GetAllConnectors().ToList() ) ;
      var result = PullBoxRouteManager.GetRouteSegments( document, route, pickInfo.Element, pullBox, heightConnector, heightWire, routeDirection, isCreateHandholeWithoutSettingHeight, nameBase, ref parentIndex, ref parentAndChildRoute,
        fromDirection, toDirection, null, false, allowedTiltedPiping ) ;

      return result ;
    }

    private bool CheckAllowedTiltedPiping( ICollection<Connector> connectors )
    {
      if ( ! connectors.Any() ) return false ;
      foreach ( var connector in connectors ) {
        var (x, y, z) = ( connector.Owner as FamilyInstance )!.FacingOrientation ;
        if ( x is not (1 or -1) && y is not (1 or -1) && z is not (1 or -1) ) {
          return true ;
        }
      }

      return false ;
    }

    protected override IReadOnlyCollection<Route> CreatePullBoxAfterRouteGenerated( Document document, RoutingExecutor executor, IReadOnlyCollection<Route> executeResultValue, PickState result )
    {
      var (_, handhole, _, _, _, _, isAutoCalculateHandholeSize, positionLabel, selectedHandhole, _, _, parentAndChildRoute) = result ;

      #region Change dimension of pullbox and set new label

      CsvStorable? csvStorable = null ;
      List<ConduitsModel>? conduitsModelData = null ;
      List<HiroiMasterModel>? hiroiMasterModels = null ;
      const string defaultHandholeLabel = "HH" ;
      if ( isAutoCalculateHandholeSize ) {
        csvStorable = document.GetCsvStorable() ;
        conduitsModelData = csvStorable.ConduitsModelData ;
        hiroiMasterModels = csvStorable.HiroiMasterModelData ;
      }

      var level = document.ActiveView.GenLevel ;
      if ( level != null ) {
        var storageDetailSymbolService = new StorageService<Level, DetailSymbolModel>( level ) ;
        var storageHandholeInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;

        PullBoxRouteManager.ChangeDimensionOfPullBoxAndSetLabel( document, handhole, csvStorable, storageDetailSymbolService, storageHandholeInfoServiceByLevel, conduitsModelData, hiroiMasterModels, defaultHandholeLabel, positionLabel,
          isAutoCalculateHandholeSize, selectedHandhole?.ConvertToPullBoxModel() ) ;
      }

      #endregion

      #region Change Representative Route Name

      if ( ! parentAndChildRoute.Any() ) return executeResultValue ;
      using Transaction transactionChangeRepresentativeRouteName = new(document) ;
      transactionChangeRepresentativeRouteName.Start( "Change Representative Route Name" ) ;
      foreach ( var (parentRouteName, childRouteNames) in parentAndChildRoute ) {
        var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => childRouteNames.Contains( c.GetRouteName()! ) ).ToList() ;
        foreach ( var conduit in conduits ) {
          conduit.TrySetProperty( RoutingParameter.RepresentativeRouteName, parentRouteName ) ;
        }
      }

      transactionChangeRepresentativeRouteName.Commit() ;

      #endregion

      return executeResultValue ;
    }

    public static IReadOnlyCollection<Route> ExecuteReRoute( Document document, RoutingExecutor executor, IProgressData progress, IReadOnlyCollection<Route> executeResultValue )
    {
      var segmentsReroute = Route.GetAllRelatedBranches( executeResultValue ).ToSegmentsWithName().ToList() ;
      using Transaction transaction = new(document) ;
      transaction.Start( "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ) ) ;
      var failureOptions = transaction.GetFailureHandlingOptions() ;
      failureOptions.SetFailuresPreprocessor( new PullBoxRouteManager.FailurePreprocessor() ) ;
      transaction.SetFailureHandlingOptions( failureOptions ) ;
      var result = executor.Run( segmentsReroute, progress ) ;
      executeResultValue = result.Value ;

      transaction.Commit( failureOptions ) ;
      return executeResultValue ;
    }
  }
}