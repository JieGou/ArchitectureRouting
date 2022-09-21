using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class HandholeRoutingCommandBase : PullBoxRoutingCommandBase
  {
    private const string DefaultBuzaicdForGradeModeThanThree = "032025" ;
    private const string DefaultKikaku = "H1" ;
    private const string HandholeName = "ハンドホール" ;
    private const string NoFamily = "There is no handhole family in this project" ;
    private const string Error = "Error" ;

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
        handholeModel = handholeModels.FirstOrDefault( model => model.Buzaicd == DefaultBuzaicdForGradeModeThanThree ) ?? handholeModels.First( handhole => handhole.Kikaku == DefaultKikaku ) ;
      }

      return new OperationResult<PickState>( new PickState( pickInfo, null, new XYZ( originX, originY, originZ ), heightConnector, heightWire, pickInfo.RouteDirection, true, false, positionLabel, handholeModel, fromDirection, toDirection, new Dictionary<string, List<string>>() ) ) ;
    }

    private List<HandholeModel> GetHandholeModels( Document document )
    {
      var csvStorable = document.GetCsvStorable() ;
      var allHandholeHiroiMasterModel = csvStorable.HiroiMasterModelData.Where( hr => hr.Hinmei.Contains( HandholeName ) ) ;
      var handholeModels = allHandholeHiroiMasterModel.Select( hiroiMasterModel => new HandholeModel( hiroiMasterModel ) ).ToList() ;
      return handholeModels.OrderBy( pb => pb.SuffixCategoryName ).ThenBy( pb => pb.PrefixCategoryName ).ThenBy( pb => pb.Width ).ThenBy( pb => pb.Height ).ToList() ;
    }
  }
}