using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB.Electrical ;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PullBoxRoutingCommandBase : RoutingCommandBase<PullBoxRoutingCommandBase.PickState>
  {
    public record PickState( PointOnRoutePicker.PickInfo PickInfo, FamilyInstance PullBox, double HeightConnector, double HeightWire, XYZ RouteDirection, bool IsCreatePullBoxWithoutSettingHeight, bool IsAutoCalculatePullBoxSize, XYZ? PositionLabel, PullBoxModel? SelectedPullBox, XYZ? FromDirection, XYZ? ToDirection, Dictionary<string, List<string>> ParentAndChildRoute ) ;
    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }
    protected virtual ConnectorFamilyType? ConnectorType => null ;
    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick point on Route", GetAddInType(), PointOnRouteFilters.RepresentativeElement ) ;
      var pullBoxViewModel = new PullBoxViewModel(document) ;
      
      var sv = new PullBoxDialog { DataContext = pullBoxViewModel } ;
      sv.ShowDialog() ;
      if ( true != sv.DialogResult ) return OperationResult<PickState>.Cancelled ;
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
      var heightConnector = pullBoxViewModel.IsCreatePullBoxWithoutSettingHeight ? originZ - level.Elevation : pullBoxViewModel.HeightConnector.MillimetersToRevitUnits() ;
      var heightWire = pullBoxViewModel.IsCreatePullBoxWithoutSettingHeight ? originZ - level.Elevation : pullBoxViewModel.HeightWire.MillimetersToRevitUnits() ;
      
      using Transaction t = new( document, "Create pull box" ) ;
      t.Start() ;
      var pullBox = PullBoxRouteManager.GenerateConnector( document, ElectricalRoutingFamilyType, ConnectorType, originX, originY, heightConnector, level, pickInfo.Route.Name ) ;
      t.Commit() ;
      
      XYZ? position ;
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;
      if ( pickInfo.Element is FamilyInstance { FacingOrientation: { } } ) {
        position = new XYZ( originX + 0.4 * baseLengthOfLine, originY + 0.7 * baseLengthOfLine, heightConnector ) ;
      } else if ( pickInfo.RouteDirection.X is 1.0 or -1.0 ) {
        position = new XYZ( originX, originY + 0.7 * baseLengthOfLine, heightConnector ) ;
      } else if ( pickInfo.RouteDirection.Y is 1.0 or -1.0 ) {
        position = new XYZ( originX + 0.4 * baseLengthOfLine, originY + 0.7 * baseLengthOfLine, heightConnector ) ;
      }
      else {
        position = new XYZ( originX, originY, heightConnector ) ;
      }

      return new OperationResult<PickState>( new PickState( pickInfo, pullBox, heightConnector, heightWire, pickInfo.RouteDirection, pullBoxViewModel.IsCreatePullBoxWithoutSettingHeight, pullBoxViewModel.IsAutoCalculatePullBoxSize, position, pullBoxViewModel.SelectedPullBox, fromDirection, toDirection, new Dictionary<string, List<string>>() ) ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      var (pickInfo, pullBox, heightConnector, heightWire, routeDirection, isCreatePullBoxWithoutSettingHeight, _, _, _, fromDirection, toDirection, parentAndChildRoute ) = pickState ;
      var route = pickInfo.SubRoute.Route ;
      var systemType = route.GetMEPSystemType() ;
      var curveType = route.UniqueCurveType ;
      var nameBase = GetNameBase( systemType, curveType! ) ;
      var parentIndex = 1 ;
      var allowedTiltedPiping = CheckAllowedTiltedPiping( document, route.RouteName ) ;
      var result = PullBoxRouteManager.GetRouteSegments( document, route, pickInfo.Element, pullBox, heightConnector, heightWire, routeDirection, isCreatePullBoxWithoutSettingHeight, nameBase, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection, null, allowedTiltedPiping ) ;

      return result ;
    }

    private bool CheckAllowedTiltedPiping( Document document, string routeName )
    {
      var routeNameArray = routeName.Split( '_' ) ;
      var mainRouteName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      var conduitsOfRouteName = document.GetAllElements<Conduit>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => {
        if ( c.GetRouteName() is not { } rName ) return false ;
        var rNameArray = rName.Split( '_' ) ;
        var strRouteName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        return strRouteName == mainRouteName ;
      } ).ToList() ;
      foreach ( var conduit in conduitsOfRouteName ) {
        var conduitLocation = ( conduit.Location as LocationCurve ) ! ;
        var conduitLine = (  conduitLocation.Curve as Line ) ! ;
        var direction = conduitLine.Direction ;
        if ( direction.X is 1 or -1 || direction.Y is 1 or -1 || direction.Z is 1 or -1 ) {
        }
        else {
          return true ;
        }
      }
      
      return false ;
    }

    protected override IReadOnlyCollection<Route> CreatePullBoxAfterRouteGenerated( Document document, RoutingExecutor executor, IReadOnlyCollection<Route> executeResultValue, PickState result )
    {
      var (_, pullBox, _, _, _, _, isAutoCalculatePullBoxSize, positionLabel, selectedPullBox, _, _, parentAndChildRoute) = result ;
      
      #region Change dimension of pullbox and set new label
      
      CsvStorable? csvStorable = null ;
      List<ConduitsModel>? conduitsModelData = null ;
      List<HiroiMasterModel>? hiroiMasterModels = null;
      if ( isAutoCalculatePullBoxSize ) {
        csvStorable = document.GetCsvStorable() ;
        conduitsModelData = csvStorable.ConduitsModelData ;
        hiroiMasterModels = csvStorable.HiroiMasterModelData ;
      }
      
      var level = document.ActiveView.GenLevel ;
      if ( level != null ) {
        var storageDetailSymbolService = new StorageService<Level, DetailSymbolModel>( level ) ;
        var storagePullBoxInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;
      
        PullBoxRouteManager.ChangeDimensionOfPullBoxAndSetLabel( document, pullBox, csvStorable, storageDetailSymbolService, storagePullBoxInfoServiceByLevel,
          conduitsModelData, hiroiMasterModels, PullBoxRouteManager.DefaultPullBoxLabel, positionLabel, isAutoCalculatePullBoxSize, selectedPullBox ) ;
      }

      #endregion
      
      #region Change Representative Route Name

      if ( ! parentAndChildRoute.Any() ) return executeResultValue ;
      using Transaction transactionChangeRepresentativeRouteName = new( document ) ;
      transactionChangeRepresentativeRouteName.Start( "Change Representative Route Name" ) ;
      foreach ( var (parentRouteName, childRouteNames ) in parentAndChildRoute ) {
        var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => childRouteNames.Contains(c.GetRouteName()! ) ).ToList() ;
        foreach ( var conduit in conduits ) {
          conduit.TrySetProperty( RoutingParameter.RepresentativeRouteName, parentRouteName ) ;
        }
      }

      transactionChangeRepresentativeRouteName.Commit() ;

      #endregion
      
      return executeResultValue ;
    }

    public static IReadOnlyCollection<Route> ExecuteReRoute( Document document, RoutingExecutor executor, IProgressData progress,
      IReadOnlyCollection<Route> executeResultValue )
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