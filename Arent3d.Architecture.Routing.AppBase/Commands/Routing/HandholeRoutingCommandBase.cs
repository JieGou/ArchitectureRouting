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
using Arent3d.Utility ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class HandholeRoutingCommandBase : RoutingCommandBase<HandholeRoutingCommandBase.PickState>
  {
    private const string DefaultBuzaicdForGradeModeThanThree = "032025" ;
    private const string HandholeName = "ハンドホール" ;
    private const string NoFamily = "There is no handhole family in this project" ;
    private const string Error = "Error" ;
    public const string DefaultHandholeLabel = "HH" ;
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

      PointOnRoutePicker.PickInfo pickInfo ;
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
        var handholeInfo = PullBoxRouteManager.GetPullBoxInfo( document, pickInfo.Route.RouteName, conduitFitting ) ;
        ( originX, originY, originZ ) = handholeInfo.Position ;
        fromDirection = handholeInfo.FromDirection ;
        toDirection = handholeInfo.ToDirection ;
      }

      var level = ( document.GetElement( pickInfo.Element.GetLevelId() ) as Level ) ! ;
      var heightConnector = originZ - level.Elevation ;
      var heightWire = originZ - level.Elevation ;

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

      HandholeModel? handholeModel = null ;
      if ( GetHandholeModels( document ) is { Count: > 0 } handholeModels ) {
        handholeModel = handholeModels.FirstOrDefault( model => model.Buzaicd == DefaultBuzaicdForGradeModeThanThree ) ?? handholeModels.First() ;
      }

      return new OperationResult<PickState>( new PickState( pickInfo, null, new XYZ( originX, originY, originZ ), heightConnector, heightWire, pickInfo.RouteDirection, true, true, positionLabel, handholeModel, fromDirection, toDirection, new Dictionary<string, List<string>>() ) ) ;
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
      var result = new List<( string RouteName, RouteSegment Segment )>() ;
      if ( pickState.Handhole == null ) return result ;

      var pickRoute = pickState.PickInfo.SubRoute.Route ;
      var routes = document.CollectRoutes( GetAddInType() ) ;
      var routeInTheSamePosition = PullBoxRouteManager.GetParentRoutesInTheSamePosition( document, routes.ToList(), pickRoute, pickState.PickInfo.Element ) ;
      var systemType = pickRoute.GetMEPSystemType() ;
      var curveType = pickRoute.UniqueCurveType ;
      var nameBase = GetNameBase( systemType, curveType! ) ;
      var parentIndex = 1 ;
      var allowedTiltedPiping = CheckAllowedTiltedPiping( pickRoute.GetAllConnectors().ToList() ) ;
      var parentAndChildRoute = new Dictionary<string, List<string>>() ;
      foreach ( var route in routeInTheSamePosition ) {
        result.AddRange( PullBoxRouteManager.GetRouteSegments( document, route, pickState.PickInfo.Element, pickState.Handhole, pickState.HeightConnector, pickState.HeightWire, pickState.RouteDirection, pickState.IsCreateHandholeWithoutSettingHeight, nameBase, ref parentIndex, ref parentAndChildRoute,
          pickState.FromDirection, pickState.ToDirection, null, false, allowedTiltedPiping ) ) ;
      }

      pickState.ParentAndChildRoute = parentAndChildRoute ;
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

    protected override void BeforeRouteGenerated( Document document, PickState result )
    {
      base.BeforeRouteGenerated( document, result ) ;
      var level = ( document.GetElement( result.PickInfo.Element.GetLevelId() ) as Level ) ! ;

      using var transaction = new Transaction( document, "Create Handhole" ) ;
      transaction.Start() ;
      result.Handhole = PullBoxRouteManager.GenerateConnector( document, ElectricalRoutingFamilyType, ConnectorType, result.HandholePosition.X, result.HandholePosition.Y, result.HeightConnector, level, result.PickInfo.Route.Name ) ;
      transaction.Commit() ;
    }

    protected override IReadOnlyCollection<Route> CreatePullBoxAfterRouteGenerated( Document document, RoutingExecutor executor, IReadOnlyCollection<Route> executeResultValue, PickState result )
    {
      #region Change dimension of handhole and set new label

      CsvStorable? csvStorable = null ;
      List<ConduitsModel>? conduitsModelData = null ;
      List<HiroiMasterModel>? hiroiMasterModels = null ;
      if ( result.IsAutoCalculateHandholeSize ) {
        csvStorable = document.GetCsvStorable() ;
        conduitsModelData = csvStorable.ConduitsModelData ;
        hiroiMasterModels = csvStorable.HiroiMasterModelData ;
      }

      var level = document.ActiveView.GenLevel ;
      StorageService<Level, DetailSymbolModel>? storageDetailSymbolService = null ;
      StorageService<Level, PullBoxInfoModel>? storageHandholeInfoServiceByLevel = null ;
      if ( level != null ) {
        storageDetailSymbolService = new StorageService<Level, DetailSymbolModel>( level ) ;
        storageHandholeInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;
      }

      PullBoxRouteManager.ChangeDimensionOfPullBoxAndSetLabel( document, result.Handhole!, csvStorable, storageDetailSymbolService, storageHandholeInfoServiceByLevel, conduitsModelData, hiroiMasterModels, DefaultHandholeLabel, result.PositionLabel, result.IsAutoCalculateHandholeSize,
        result.SelectedHandhole?.ConvertToPullBoxModel() ) ;

      #endregion

      #region Change Representative Route Name

      if ( ! result.ParentAndChildRoute.Any() ) return executeResultValue ;
      using Transaction transactionChangeRepresentativeRouteName = new(document) ;
      transactionChangeRepresentativeRouteName.Start( "Change Representative Route Name" ) ;
      foreach ( var (parentRouteName, childRouteNames) in result.ParentAndChildRoute ) {
        var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => childRouteNames.Contains( c.GetRouteName()! ) ).ToList() ;
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
      public FamilyInstance? Handhole { get ; set ; }
      public XYZ HandholePosition { get ; set ; }
      public double HeightConnector { get ; set ; }
      public double HeightWire { get ; set ; }
      public XYZ RouteDirection { get ; set ; }
      public bool IsCreateHandholeWithoutSettingHeight { get ; set ; }
      public bool IsAutoCalculateHandholeSize { get ; set ; }
      public XYZ? PositionLabel { get ; set ; }
      public HandholeModel? SelectedHandhole { get ; set ; }
      public XYZ? FromDirection { get ; set ; }
      public XYZ? ToDirection { get ; set ; }
      public Dictionary<string, List<string>> ParentAndChildRoute { get ; set ; }

      public PickState( PointOnRoutePicker.PickInfo pickInfo, FamilyInstance? handhole, XYZ handholePosition, double heightConnector, double heightWire, XYZ routeDirection, bool isCreateHandholeWithoutSettingHeight, bool isAutoCalculateHandholeSize, XYZ? positionLabel, HandholeModel? selectedHandhole,
        XYZ? fromDirection, XYZ? toDirection, Dictionary<string, List<string>> parentAndChildRoute )
      {
        PickInfo = pickInfo ;
        Handhole = handhole ;
        HandholePosition = handholePosition ;
        HeightConnector = heightConnector ;
        HeightWire = heightWire ;
        RouteDirection = routeDirection ;
        IsCreateHandholeWithoutSettingHeight = isCreateHandholeWithoutSettingHeight ;
        IsAutoCalculateHandholeSize = isAutoCalculateHandholeSize ;
        PositionLabel = positionLabel ;
        SelectedHandhole = selectedHandhole ;
        FromDirection = fromDirection ;
        ToDirection = toDirection ;
        ParentAndChildRoute = parentAndChildRoute ;
      }
    }
  }
}