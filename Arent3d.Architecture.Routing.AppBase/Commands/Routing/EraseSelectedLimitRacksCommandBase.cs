using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseSelectedLimitRacksCommandBase : EraseLimitRackCommandBase
  {
    private const double Tolerance = 0.1 ;

    protected override (IEnumerable<string> rackIds, IEnumerable<string> detailCurverIds, IEnumerable<LimitRackModel>
      limitRackModels) GetLimitRackIds( UIDocument ui, Document doc, LimitRackStorable limitRackStorable )
    {
      var selectedConduitAndConduitFittings = ui.Selection
        .PickElementsByRectangle( ConduitAndConduitFittingSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).ToList() ;

      // CollectHiddenConduitIn2dView( doc, selectedConduitAndConduitFittings ) ;

      var selectedLimitRackModels =
        (GetSelectedLimitRackModel( limitRackStorable, selectedConduitAndConduitFittings ) ?? Array.Empty<LimitRackModel>()).ToList() ;
      var rackIds = new HashSet<string>() ;
      selectedLimitRackModels.ForEach( model =>  rackIds.AddRange( model.RackIds ) ) ;
      var detailLineIds = selectedConduitAndConduitFittings.Where( detailLine => detailLine is DetailLine ).Select( detailLine => detailLine.UniqueId )
        .ToList() ;

      // CollectHiddenCableTrayIn2dView( doc, selectedConduitAndConduitFittings, rackIds ) ;

      return new ValueTuple<IEnumerable<string>, IEnumerable<string>, IEnumerable<LimitRackModel>>( rackIds,
        detailLineIds, selectedLimitRackModels ) ;
    }

    protected override void RemoveLimitRackModels( LimitRackStorable limitRackStorable,
      IEnumerable<LimitRackModel> selectedLimitRackModels )
    {
      selectedLimitRackModels.ForEach( x => limitRackStorable.LimitRackModelData.Remove( x ) ) ;
      limitRackStorable.Save() ;
    }

    private static IEnumerable<LimitRackModel>? GetSelectedLimitRackModel( LimitRackStorable? limitRackStorable,
      IEnumerable<Element> selectedConduitAndConduitFittings )
    {
      var allSelectedRoute = selectedConduitAndConduitFittings.Select( x => x.GetRouteName() ).Distinct() ;

      var selectedRoute = allSelectedRoute as string?[] ?? allSelectedRoute.ToArray() ;
      if (!selectedRoute.Any()) yield break;
      
      if ( limitRackStorable == null || ! limitRackStorable.LimitRackModelData.Any() ) {
        yield break ;
      }

      var allSelectedLimitRackModel =
        limitRackStorable.LimitRackModelData.Where( lmd => selectedRoute.Any( route => route == lmd.RouteName ) ) ;

      var selectedLimitRackModel = allSelectedLimitRackModel as LimitRackModel[] ?? allSelectedLimitRackModel.ToArray() ;
      if (!selectedLimitRackModel.Any()) yield break;
      
      foreach ( var limitRackModel in selectedLimitRackModel ) {
        yield return limitRackModel ;
      }
      // foreach ( var limitRackModel in from limitRackModel in limitRackStorable.LimitRackModelData
      //          let conduitAndConduitFittings =
      //            selectedConduitAndConduitFittings as Element[] ?? selectedConduitAndConduitFittings.ToArray()
      //          where conduitAndConduitFittings.Any( x => x.UniqueId == limitRackModel.ConduitId )
      //          select limitRackModel ) {
      //   yield return limitRackModel ;
      // }
    }

    private static void CollectHiddenCableTrayIn2dView( Document doc, List<Element> selectedConduitAndConduitFittings,
      ISet<string> selectedCableTrayIds )
    {
      var allCableRays = doc.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTray ) ;
      foreach ( var cableTray in allCableRays ) {
        if ( selectedCableTrayIds.Contains( cableTray.UniqueId ) )
          continue ;
        foreach ( var selectedConduitAndConduitFitting in selectedConduitAndConduitFittings ) {
          if ( cableTray.Location is LocationPoint cableTrayPoint &&
               selectedConduitAndConduitFitting.Location is LocationCurve
               {
                 Curve: Line line
               } && cableTrayPoint.Point.DistanceTo( line.Origin ) < Tolerance ) {
            selectedCableTrayIds.Add( cableTray.UniqueId ) ;
          }
        }
      }
    }

    private static void CollectHiddenConduitIn2dView( Document doc, List<Element> selectedConduitAndConduitFittings )
    {
      var selectedConnectors = selectedConduitAndConduitFittings
        .Where( ConduitAndConduitFittingSelectionFilter.IsConnector ).ToList() ;

      var elements = doc.GetAllElementsOfRoute<MEPCurve>() ;

      var allConduits = new List<Element>() ;

      foreach ( var element in elements ) {
        var fromConnector = element.GetNearestEndPoints( true ) ;
        foreach ( var endPoint in fromConnector ) {
          if ( endPoint is not ConnectorEndPoint connectorEndPoint ) continue ;
          if ( selectedConnectors.Any( connector => connector.UniqueId == connectorEndPoint.EquipmentUniqueId ) )
            allConduits.Add( element ) ;
        }
      }

      foreach ( var element in allConduits.Where( element => ! selectedConduitAndConduitFittings.Contains( element ) ) ) {
        selectedConduitAndConduitFittings.Add( element ) ;
      }
    }
  }
}