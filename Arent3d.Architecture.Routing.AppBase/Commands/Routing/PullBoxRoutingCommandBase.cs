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
using Arent3d.Utility ;
using Autodesk.Revit.Exceptions ;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PullBoxRoutingCommandBase : RoutingCommandBase<PullBoxRoutingCommandBase.PickState>
  {
    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }
    protected virtual ConnectorFamilyType? ConnectorType => null ;
    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      PointOnRoutePicker.PickInfo? pickInfo ;

      try {
        pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick point on Route", GetAddInType() ) ;
      }
      catch ( OperationCanceledException ) {
        return OperationResult<PickState>.Cancelled ;
      }
      
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

      XYZ? positionLabel ;
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;
      if ( pickInfo.Element is FamilyInstance { FacingOrientation: { } } )
        positionLabel = new XYZ( originX + 0.4 * baseLengthOfLine, originY + 0.7 * baseLengthOfLine, heightConnector ) ;
      else if ( pickInfo.RouteDirection.X is 1.0 or -1.0 )
        positionLabel = new XYZ( originX, originY + 0.7 * baseLengthOfLine, heightConnector ) ;
      else if ( pickInfo.RouteDirection.Y is 1.0 or -1.0 )
        positionLabel = new XYZ( originX + 0.4 * baseLengthOfLine, originY + 0.7 * baseLengthOfLine, heightConnector ) ;
      else
        positionLabel = new XYZ( originX, originY, heightConnector ) ;

      return new OperationResult<PickState>( new PickState( pickInfo, null, new XYZ( originX, originY, originZ ), heightConnector, heightWire, pickInfo.RouteDirection, pullBoxViewModel.IsCreatePullBoxWithoutSettingHeight, pullBoxViewModel.IsAutoCalculatePullBoxSize, positionLabel, pullBoxViewModel.SelectedPullBox, fromDirection, toDirection, new Dictionary<string, List<string>>() ) ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      var result = new List<( string RouteName, RouteSegment Segment )>() ;
      if ( pickState.PullBox == null ) return result ;
      
      var pickRoute = pickState.PickInfo.SubRoute.Route ;
      var routes = document.CollectRoutes( GetAddInType()) ;
      var routeInTheSamePosition = PullBoxRouteManager.GetParentRoutesInTheSamePosition( document, routes.ToList(), pickRoute, pickState.PickInfo.Element ) ;
      var systemType = pickRoute.GetMEPSystemType() ;
      var curveType = pickRoute.UniqueCurveType ;
      var nameBase = GetNameBase( systemType, curveType! ) ;
      var parentIndex = 1 ;
      var allowedTiltedPiping = CheckAllowedTiltedPiping( pickRoute.GetAllConnectors().ToList() ) ;
      var parentAndChildRoute = new Dictionary<string, List<string>>() ;
      foreach ( var route in routeInTheSamePosition ) {
        result.AddRange( PullBoxRouteManager.GetRouteSegments( document, route, pickState.PickInfo.Element, pickState.PullBox!, pickState.HeightConnector,
          pickState.HeightWire, pickState.RouteDirection, pickState.IsCreatePullBoxWithoutSettingHeight, nameBase, ref parentIndex,
          ref parentAndChildRoute, pickState.FromDirection, pickState.ToDirection, null, false, allowedTiltedPiping ) );
      }

      pickState.ParentAndChildRoute = parentAndChildRoute ;
      return result ;
    }

    private bool CheckAllowedTiltedPiping( ICollection<Connector> connectors )
    {
      if ( ! connectors.Any() ) return false ;
      foreach ( var connector in connectors ) {
        var (x, y, z) = ( connector.Owner as FamilyInstance )!.FacingOrientation ;
        if ( x is not ( 1 or -1 ) && y is not ( 1 or -1 ) && z is not ( 1 or -1 ) ) {
          return true ;
        }
      }
      
      return false ;
    }

    protected override void BeforeRouteGenerated( Document document, PickState result )
    {
      base.BeforeRouteGenerated( document, result ) ;
      var level = ( document.GetElement( result.PickInfo.Element.GetLevelId() ) as Level ) ! ;

      using var transaction = new Transaction( document, "Create pull box" ) ;
      transaction.Start() ;
      result.PullBox = PullBoxRouteManager.GenerateConnector( document, ElectricalRoutingFamilyType, ConnectorType, result.PullBoxPosition.X, result.PullBoxPosition.Y, result.HeightConnector, level, result.PickInfo.Route.Name ) ;
      transaction.Commit() ;
    }

    protected override IReadOnlyCollection<Route> CreatePullBoxAfterRouteGenerated( Document document, RoutingExecutor executor, IReadOnlyCollection<Route> executeResultValue, PickState result )
    {
      #region Change dimension of pullbox and set new label

      CsvStorable? csvStorable = null ;
      List<ConduitsModel>? conduitsModelData = null ;
      List<HiroiMasterModel>? hiroiMasterModels = null ;
      if ( result.IsAutoCalculatePullBoxSize ) {
        csvStorable = document.GetCsvStorable() ;
        conduitsModelData = csvStorable.ConduitsModelData ;
        hiroiMasterModels = csvStorable.HiroiMasterModelData ;
      }

      var level = document.ActiveView.GenLevel ;
      StorageService<Level, DetailSymbolModel>? storageDetailSymbolService = null ;
      StorageService<Level, PullBoxInfoModel>? storagePullBoxInfoServiceByLevel = null ;
      if ( level != null ) {
        storageDetailSymbolService = new StorageService<Level, DetailSymbolModel>( level ) ;
        storagePullBoxInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;
      }

      PullBoxRouteManager.ChangeDimensionOfPullBoxAndSetLabel( document, result.PullBox!, csvStorable, storageDetailSymbolService, storagePullBoxInfoServiceByLevel, conduitsModelData, hiroiMasterModels, PullBoxRouteManager.DefaultPullBoxLabel, result.PositionLabel, result.IsAutoCalculatePullBoxSize, result.SelectedPullBox ) ;

      #endregion
      
      #region Change Representative Route Name

      if ( ! result.ParentAndChildRoute.Any() ) return executeResultValue ;
      using Transaction transactionChangeRepresentativeRouteName = new( document ) ;
      transactionChangeRepresentativeRouteName.Start( "Change Representative Route Name" ) ;
      foreach ( var (parentRouteName, childRouteNames ) in result.ParentAndChildRoute ) {
        var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => childRouteNames.Contains(c.GetRouteName()! ) ).ToList() ;
        foreach ( var conduit in conduits ) {
          conduit.TrySetProperty( RoutingParameter.RepresentativeRouteName, parentRouteName ) ;
        }
      }

      transactionChangeRepresentativeRouteName.Commit() ;

      #endregion
      
      return executeResultValue ;
    }

    public class PickState
    {
      public PointOnRoutePicker.PickInfo PickInfo { get ; set ; }
      public FamilyInstance? PullBox { get ; set ; }
      public XYZ PullBoxPosition { get ; set ; }
      public double HeightConnector { get ; set ; }
      public double HeightWire { get ; set ; }
      public XYZ RouteDirection { get ; set ; }
      public bool IsCreatePullBoxWithoutSettingHeight { get ; set ; }
      public bool IsAutoCalculatePullBoxSize { get ; set ; }
      public XYZ? PositionLabel { get ; set ; }
      public PullBoxModel? SelectedPullBox { get ; set ; }
      public XYZ? FromDirection { get ; set ; }
      public XYZ? ToDirection { get ; set ; }
      public Dictionary<string, List<string>> ParentAndChildRoute { get ; set ; }

      public PickState( PointOnRoutePicker.PickInfo pickInfo, FamilyInstance? pullBox, XYZ pullBoxPosition, double heightConnector, double heightWire, XYZ routeDirection, bool isCreatePullBoxWithoutSettingHeight, bool isAutoCalculatePullBoxSize, XYZ? positionLabel, PullBoxModel? selectedPullBox, XYZ? fromDirection, XYZ? toDirection, Dictionary<string, List<string>> parentAndChildRoute )
      {
        PickInfo = pickInfo ;
        PullBox = pullBox ;
        PullBoxPosition = pullBoxPosition ;
        HeightConnector = heightConnector ;
        HeightWire = heightWire ;
        RouteDirection = routeDirection ;
        IsCreatePullBoxWithoutSettingHeight = isCreatePullBoxWithoutSettingHeight ;
        IsAutoCalculatePullBoxSize = isAutoCalculatePullBoxSize ;
        PositionLabel = positionLabel ;
        SelectedPullBox = selectedPullBox ;
        FromDirection = fromDirection ;
        ToDirection = toDirection ;
        ParentAndChildRoute = parentAndChildRoute ;
      }
    }
  }
}