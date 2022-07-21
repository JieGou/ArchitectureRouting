using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using MoreLinq ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseSelectedLimitRacksCommandBase : EraseLimitRackCommandBase
  {
    protected override IEnumerable<string> GetLimitRackIds( UIDocument ui, Document doc )
    {
      var selectedLimitRackRefElements = ui.Selection.PickElementsByRectangle( ConduitAndConduitFittingSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).ToList() ;

      var allRackInstances = GetAllLimitRackInstance( doc ).ToList() ;

      var allRackIds = new HashSet<string>() ;

      foreach ( var rackIds in from selectedElement in selectedLimitRackRefElements
               where ConduitAndConduitFittingSelectionFilter.IsConduitOrConduitFitting( selectedElement )
               select GetRackIdAtConduitOrConduitFittings( selectedElement, allRackInstances )
               into selectedRackIds
               where selectedRackIds.Any()
               select selectedRackIds ) {
        allRackIds.AddRange( rackIds ) ;
      }

      return allRackIds ;
    }

    protected override IEnumerable<string> GetBoundaryCableTraysFromLimitRacks( Document doc, IEnumerable<string> limitRackIds )
    {
      var boundaryCableTrays = new FilteredElementCollector( doc )
        .OfClass( typeof( CurveElement ) )
        .OfType<CurveElement>()
        .Where( x => null != x.LineStyle && ( x.LineStyle as GraphicsStyle )!.GraphicsStyleCategory.Name == BoundaryCableTrayLineStyleName )
        .Select( x => x )
        .ToList() ;

      var limitRacks = limitRackIds.Select( doc.GetElement ) ;

      foreach ( var boundaryCableTray in from boundaryCableTray in boundaryCableTrays from limitRack in limitRacks where IsCableTrayAndBoundaryCableTrayCollisionTogether( boundaryCableTray, limitRack ) select boundaryCableTray ) {
        yield return boundaryCableTray.UniqueId ;
      }
    }

    private static bool IsCableTrayAndBoundaryCableTrayCollisionTogether(CurveElement cableTrayBoundary, Element limitRackElement)
    {
      var cableTraySolids = limitRackElement.GetFineSolids();

      return false ;
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
        return  conduit.ConnectorManager.Connectors.Cast<Connector>().ToList() ;
      }

      return element is not FamilyInstance conduitFittingInstance ? new List<Connector>() : conduitFittingInstance.MEPModel.ConnectorManager.Connectors.Cast<Connector>().ToList() ;
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