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
    protected override (IEnumerable<string> rackIds,IEnumerable<string> detailCurverIds,IEnumerable<LimitRackModel> limitRackModels) GetLimitRackIds( UIDocument ui, Document doc,LimitRackStorable limitRackStorable )
    {
      var selectedConduitAndConduitFittings = ui.Selection
        .PickElementsByRectangle( ConduitAndConduitFittingSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).ToList() ;
      
      CollectHiddenConduitIn2dView( doc,selectedConduitAndConduitFittings);

      var selectedLimitRackModels = GetSelectedLimitRackModel( limitRackStorable,selectedConduitAndConduitFittings).ToList() ;
      var rackIds = selectedLimitRackModels.Select( x => x.RackId ).Distinct().ToHashSet() ;
      var detailLineIds = selectedConduitAndConduitFittings.Where( x => x is DetailLine ).Select( x => x.UniqueId ).ToList() ;
      
      CollectHiddenCableTrayIn2dView( doc,selectedConduitAndConduitFittings,rackIds );
      
      return new ValueTuple<IEnumerable<string>, IEnumerable<string>,IEnumerable<LimitRackModel>>( rackIds,detailLineIds,selectedLimitRackModels )  ;
    }
    
    

    protected override void RemoveLimitRackModels( LimitRackStorable limitRackStorable, IEnumerable<LimitRackModel> selectedLimitRackModels )
    {
      selectedLimitRackModels.ForEach( x=>limitRackStorable.LimitRackModelData.Remove( x ) );
      limitRackStorable.Save();
    }

    private static IEnumerable<LimitRackModel> GetSelectedLimitRackModel(LimitRackStorable? limitRackStorable, IEnumerable<Element> selectedConduitAndConduitFittings )
    {
      
      
      if ( limitRackStorable == null || ! limitRackStorable.LimitRackModelData.Any() ) {
        yield break;
      }

      foreach ( var limitRackModel in from limitRackModel in limitRackStorable.LimitRackModelData let conduitAndConduitFittings = selectedConduitAndConduitFittings as Element[] ?? selectedConduitAndConduitFittings.ToArray() where conduitAndConduitFittings.Any( x => x.UniqueId == limitRackModel.ConduitId ) select limitRackModel ) {
        yield return limitRackModel ;
      }
    }

    private void CollectHiddenCableTrayIn2dView(Document doc, List<Element> selectedConduitAndConduitFittings ,HashSet<string> selectedCabetrayIds )
    {
      var allCableRays = doc.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTray ) ;
      foreach ( var cableTray in allCableRays ) {
        if(selectedCabetrayIds.Contains( cableTray.UniqueId ))
          continue;
        foreach ( var selectedConduitAndConduitFitting in selectedConduitAndConduitFittings ) {
          if ( cableTray.Location is LocationPoint cableTrayPoint && selectedConduitAndConduitFitting.Location is LocationCurve
              {
                Curve: Line line
              } && cableTrayPoint.Point.DistanceTo( line.Origin )<Tolerance) 
          {
            selectedCabetrayIds.Add( cableTray.UniqueId );
          }
        }
      }
    }

    private void CollectHiddenConduitIn2dView(Document doc, List<Element> selectedConduitAndConduitFittings)
    {
      var selectedConnectors =
        selectedConduitAndConduitFittings.Where( element =>
          ConduitAndConduitFittingSelectionFilter.IsConnector( element ) ).ToList() ;

      var elements = doc.GetAllElementsOfRoute<MEPCurve>() ;

      var hiddingConduit = new List<Element>() ;

      foreach ( var element in elements ) {
        var fromConnector = element.GetNearestEndPoints( true ) ;
        foreach ( var endPoint in fromConnector ) {
          if (endPoint is not ConnectorEndPoint connectorEndPoint) continue;
          if (selectedConnectors.Any(connector=>connector.UniqueId == connectorEndPoint.EquipmentUniqueId))
            hiddingConduit.Add( element );
        }
      }

      foreach ( var element in hiddingConduit ) {
        if (selectedConduitAndConduitFittings.Contains( element ))
          continue;
        selectedConduitAndConduitFittings.Add( element );
      }
    }
  }
}