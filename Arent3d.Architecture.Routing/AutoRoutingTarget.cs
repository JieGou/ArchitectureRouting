using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Core ;
using Arent3d.Architecture.Routing.Core.Conditions ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  public class AutoRoutingTarget : IAutoRoutingTarget
  {
    /// <summary>
    /// A <see cref="Route"/> which an <see cref="AutoRoutingTarget"/> belongs to.
    /// </summary>
    public Route Route { get ; }

    /// <summary>
    /// Routing end points which fluid flows from.
    /// </summary>
    private readonly IReadOnlyCollection<EndPoint> _fromEndPoints ;

    /// <summary>
    /// Routing end points which fluid flows to.
    /// </summary>
    private readonly IReadOnlyCollection<EndPoint> _toEndPoints ;

    /// <summary>
    /// Returns all routing end points.
    /// </summary>
    public IEnumerable<EndPoint> EndPoints => _fromEndPoints.Concat( _toEndPoints ) ;

    public AutoRoutingTarget( Document document, Route route )
    {
      Route = route ;
      _fromEndPoints = ConvertToEndPoints( document, route.FromElementIds, true ) ;
      _toEndPoints = ConvertToEndPoints( document, route.ToElementIds, false ) ;

      Condition = new AutoRoutingCondition( document, Route ) ;
    }

    private static IReadOnlyCollection<EndPoint> ConvertToEndPoints( Document document, IEnumerable<ConnectorIds> connectorIds, bool isStart )
    {
      var connectors = connectorIds.Select( id => document.FindConnector( id.ElementId, id.ConnectorId ) ).NonNull() ;
      return connectors.Select( connector =>
      {
        var endPoint = ConnectorMapper.Instance.Get( connector ) ;
        endPoint.IsStart = isStart ;
        return endPoint ;
      } ).EnumerateAll() ;
    }

    public IAutoRoutingSpatialConstraints? CreateConstraints()
    {
      if ( ( 0 < _fromEndPoints.Count ) && ( 0 < _toEndPoints.Count ) ) {
        return new AutoRoutingSpatialConstraints( _fromEndPoints, _toEndPoints ) ;
      }

      return null ;
    }

    public string LineId => Route.RouteId ;

    public IAutoRoutingCondition Condition { get ; }

    public int RouteCount => _fromEndPoints.Count + _toEndPoints.Count - 1 ;

    public Action<IEnumerable<(IAutoRoutingEndPoint, Vector3d)>> PositionInitialized => SyncTermPositions ;

    private void SyncTermPositions( IEnumerable<(IAutoRoutingEndPoint, Vector3d)> positions )
    {
      foreach ( var (autoRoutingEndPoint, position) in positions ) {
        if ( ! ( autoRoutingEndPoint is EndPoint endPoint ) ) throw new Exception() ;

        endPoint.SetPosition( position ) ;
      }
    }


    #region Inner classes

    private class AutoRoutingCondition : IAutoRoutingCondition
    {
      private readonly Route _route ;

      public AutoRoutingCondition( Document document, Route route )
      {
        _route = route ;
        IsRoutingOnPipeRacks = DocumentMapper.Instance.Get( document ).IsRoutingOnPipeRacks( route ) ;
      }

      public bool IsRoutingOnPipeRacks { get ; }
      public LineType Type => _route.ServiceType ;
      public string FluidPhase => _route.FluidPhase ;
      public int Priority => _route.Priority ;
      public string GroupName => string.Empty ;
      public LoopType LoopType => _route.LoopType ;
      public ProcessConstraint ProcessConstraint => _route.ProcessConstraint ;
    }

    private class AutoRoutingSpatialConstraints : IAutoRoutingSpatialConstraints
    {
      public AutoRoutingSpatialConstraints( IReadOnlyCollection<EndPoint> fromEndPoints, IReadOnlyCollection<EndPoint> toEndPoints )
      {
        Starts = fromEndPoints ;
        Destination = toEndPoints ;
      }

      public IEnumerable<IAutoRoutingEndPoint> Starts { get ; }

      public IEnumerable<IAutoRoutingEndPoint> Destination { get ; }
    }

    #endregion
  }
}