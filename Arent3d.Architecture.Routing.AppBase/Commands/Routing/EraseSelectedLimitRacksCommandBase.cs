using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseSelectedLimitRacksCommandBase : EraseLimitRackCommandBase
  {
    protected override (IEnumerable<string> rackIds, IEnumerable<string>? detailCurverIds) GetLimitRackIds(
      UIDocument ui, Document doc )
    {
      var selectedConduitAndConduitFittings = ui.Selection
        .PickElementsByRectangle( ConduitAndConduitFittingSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).ToList() ;

      var allRackInstances = GetAllLimitRackInstance( doc ).ToList() ;

      var allRackIds = new HashSet<string>() ;

      foreach ( var rackId in from selectedElement in selectedConduitAndConduitFittings
               where ConduitAndConduitFittingSelectionFilter.IsConduitOrConduitFitting( selectedElement )
               select GetRackIdAtConduitOrConduitFittings( selectedElement, allRackInstances )
               into rackIds
               where rackIds.Any()
               select rackIds ) {
        allRackIds.AddRange( rackId ) ;
      }

      var detailLineIds = selectedConduitAndConduitFittings.Where( detailLine => detailLine is DetailLine )
        .Select( detailLine => detailLine.UniqueId ).ToList() ;

      return new ValueTuple<IEnumerable<string>, IEnumerable<string>>( allRackIds, detailLineIds ) ;
    }

    private static IEnumerable<string> GetRackIdAtConduitOrConduitFittings( Element element,
      IReadOnlyCollection<FamilyInstance> allRackInstances )
    {
      var connectors = GetConnectorsOfConduitOrConduitFitting( element ) ;
      if ( ! connectors.Any() ) yield break ;
      foreach ( var connector in connectors ) {
        var rackIds = GetRackIdAtConnector( connector, allRackInstances ).ToList() ;
        if ( ! rackIds.Any() ) continue ;
        foreach ( var rackId in rackIds ) {
          yield return rackId ;
        }
      }
    }

    private static IList<Connector> GetConnectorsOfConduitOrConduitFitting( Element element )
    {
      if ( element is Conduit conduit ) {
        var conduitConnectors = conduit.ConnectorManager.Connectors.Cast<Connector>().ToList() ;
        return conduitConnectors ;
      }

      if ( element is not FamilyInstance conduitFittingInstance )
        return new List<Connector>() ;

      var conduitFittingConnectors =
        conduitFittingInstance.MEPModel.ConnectorManager.Connectors.Cast<Connector>().ToList() ;
      return conduitFittingConnectors ;
    }

    private static IEnumerable<string> GetRackIdAtConnector( Connector connector,
      IEnumerable<FamilyInstance> allRackInstances )
    {
      return from rackInstance in allRackInstances
        let connectors = rackInstance.MEPModel.ConnectorManager.Connectors.Cast<Connector>()
        where connectors.Any( c => IsTwoConnectorSamePlace( connector, c ) )
        select rackInstance.UniqueId ;
    }

    private static bool IsTwoConnectorSamePlace( Connector firstConnector, Connector anotherConnector )
    {
      const double epsilon = 0.1d ;
      return ! ( firstConnector.Origin.DistanceTo( anotherConnector.Origin ) > epsilon ) ;
    }
  }
}