using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Core ;
using Arent3d.Architecture.Routing.Core.Conditions ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Route definition class.
  /// </summary>
  public class Route
  {
    public const string DefaultFluidPhase = "None" ;
    public const string DefaultInsulationType = "None" ;
    
    /// <summary>
    /// Unique identifier of a route.
    /// </summary>
    public string RouteId { get ; }

    /// <summary>
    /// Connector's element ids where this route starts with.
    /// </summary>
    public ICollection<ConnectorIds> FromElementIds { get ; private set ; } = new List<ConnectorIds>() ;
    /// <summary>
    /// Connector's element ids where this route ends with.
    /// </summary>
    public ICollection<ConnectorIds> ToElementIds { get ; private set ; } = new List<ConnectorIds>() ;

    public string FluidPhase => DefaultFluidPhase ;
    public LineType ServiceType => LineType.Utility ;
    public LoopType LoopType => LoopType.Non ;
    public ProcessConstraint ProcessConstraint => ProcessConstraint.None ;
    public int Priority { get ; set ; }

    public Route( string routeId )
    {
      RouteId = routeId ;
    }

    /// <summary>
    /// When connector's element ids are duplicated, the second or latter are erased. An element id which is registered both from and to is uniformed, the one registered in FromElementIds is remain.
    /// </summary>
    public void RemoveDuplicatedElementIds()
    {
      var set = new HashSet<ConnectorIds>( FromElementIds.Count + ToElementIds.Count ) ;

      var newFromList = new List<ConnectorIds>( FromElementIds.Count ) ;
      var newToList = new List<ConnectorIds>( ToElementIds.Count ) ;

      foreach ( var id in FromElementIds ) {
        if ( set.Add( id ) ) newFromList.Add( id ) ;
      }
      foreach ( var id in ToElementIds ) {
        if ( set.Add( id ) ) newToList.Add( id ) ;
      }

      FromElementIds = newFromList ;
      ToElementIds = newToList ;
    }
  }
}