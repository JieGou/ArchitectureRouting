using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PullBoxRoutingCommandBase : RoutingCommandBase<PullBoxRoutingCommandBase.PickState>
  {
    public record PickState( PointOnRoutePicker.PickInfo PickInfo, FamilyInstance PullBox, double HeightConnector, double HeightWire, XYZ RouteDirection, bool IsCreatePullBoxWithoutSettingHeight, bool IsAutoCalculatePullBoxSize, PullBoxModel? SelectedPullBox, XYZ? FromDirection, XYZ? ToDirection ) ;
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

      using Transaction t2 = new( document, "Create text note" ) ;
      t.Start() ;
      XYZ? position ;
      if ( pickInfo.Element is FamilyInstance { FacingOrientation: { } } ) {
        position = new XYZ( originX + 0.2, originY + 0.5, heightConnector ) ;
      } else if ( pickInfo.RouteDirection.X is 1.0 or -1.0 ) {
        position = new XYZ( originX, originY + 0.5, heightConnector ) ;
      } else if ( pickInfo.RouteDirection.Y is 1.0 or -1.0 ) {
        position = new XYZ( originX + 0.2, originY + 0.2, heightConnector ) ;
      }
      else {
        position = new XYZ( originX, originY, heightConnector ) ;
      }

      PullBoxRouteManager.CreateTextNoteAndGroupWithPullBox( document, position , pullBox, "PB" );
      t.Commit() ;

      return new OperationResult<PickState>( new PickState( pickInfo, pullBox, heightConnector, heightWire, pickInfo.RouteDirection, pullBoxViewModel.IsCreatePullBoxWithoutSettingHeight, pullBoxViewModel.IsAutoCalculatePullBoxSize, pullBoxViewModel.SelectedPullBox, fromDirection, toDirection ) ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      var (pickInfo, pullBox, heightConnector, heightWire, routeDirection, isCreatePullBoxWithoutSettingHeight, isAutoCalculatePullBoxSize, selectedPullBox, fromDirection, toDirection ) = pickState ;
      var route = pickInfo.SubRoute.Route ;
      var systemType = route.GetMEPSystemType() ;
      var curveType = route.UniqueCurveType ;
      var nameBase = GetNameBase( systemType, curveType! ) ;
      var result = PullBoxRouteManager.GetRouteSegments( document, route, pickInfo.Element, pullBox, heightConnector, heightWire, routeDirection, isCreatePullBoxWithoutSettingHeight, nameBase, fromDirection, toDirection ) ;
      
      return result ;
    }

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, PickState result )
    {
      base.AfterRouteGenerated( document, executeResultValue, result ) ;
      
      var (_, pullBox, _, _, _, _, isAutoCalculatePullBoxSize, selectedPullBox, _, _ ) = result ;
      
      #region Change dimension of pullbox and set new label
      var detailSymbolStorable = document.GetDetailSymbolStorable() ;
      
      string buzaiCd = string.Empty ;
      string textLabel = PullBoxRouteManager.DefaultPullBoxLabel ;
      if ( isAutoCalculatePullBoxSize ) {
        var csvStorable = document.GetCsvStorable() ;
        var conduitsModelData = csvStorable.ConduitsModelData ;
        var hiroiMasterModels = csvStorable.HiroiMasterModelData ;
        var pullBoxModel = PullBoxRouteManager.GetPullBoxWithAutoCalculatedDimension( document, pullBox, csvStorable, detailSymbolStorable, conduitsModelData, hiroiMasterModels ) ;
        if ( pullBoxModel != null ) {
          buzaiCd = pullBoxModel.Buzaicd;
          var (depth, width, _) =  PullBoxRouteManager.ParseKikaku( pullBoxModel.Kikaku );
          textLabel = PullBoxRouteManager.GetPullBoxTextBox( depth, width, PullBoxRouteManager.DefaultPullBoxLabel ) ;
        }
      }
      else {
        if(selectedPullBox != null)
          buzaiCd = selectedPullBox.Buzaicd ;   
      }
      
      if ( ! string.IsNullOrEmpty( buzaiCd ) ) {
        using Transaction t1 = new(document, "Update dimension of pull box") ;
        t1.Start() ;
        pullBox.ParametersMap.get_Item( PickUpViewModel.MaterialCodeParameter )?.Set( buzaiCd ) ;
        detailSymbolStorable.DetailSymbolModelData.RemoveAll( _ => true) ;
        t1.Commit() ;
        
        ChangePullBoxDimensionCommandBase.ChangeLabelOfPullBox( document, pullBox, textLabel ) ;
      }
      #endregion
    }
  }
}