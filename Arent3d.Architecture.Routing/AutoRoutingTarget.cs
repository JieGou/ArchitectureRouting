using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoint ;
using Arent3d.Routing ;
using Arent3d.Routing.Conditions ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  public class AutoRoutingTarget : IAutoRoutingTarget
  {
    /// <summary>
    /// A <see cref="SubRoute"/> which an <see cref="AutoRoutingTarget"/> belongs to.
    /// </summary>
    public SubRoute SubRoute { get ; }

    /// <summary>
    /// Routing end points which fluid flows from.
    /// </summary>
    private readonly IReadOnlyCollection<EndPointBase> _fromEndPoints ;

    /// <summary>
    /// Routing end points which fluid flows to.
    /// </summary>
    private readonly IReadOnlyCollection<EndPointBase> _toEndPoints ;

    /// <summary>
    /// Returns all routing end points.
    /// </summary>
    public IEnumerable<EndPointBase> EndPoints => _fromEndPoints.Concat( _toEndPoints ) ;

    public AutoRoutingTarget( Document document, SubRoute subRoute, int priority )
    {
      SubRoute = subRoute ;
      _fromEndPoints = subRoute.GetFromEndPoints( document ).EnumerateAll() ;
      _toEndPoints = subRoute.GetToEndPoints( document ).EnumerateAll() ;

      Condition = new AutoRoutingCondition( document, SubRoute, priority ) ;
    }

    public IAutoRoutingSpatialConstraints? CreateConstraints()
    {
      if ( ( 0 < _fromEndPoints.Count ) && ( 0 < _toEndPoints.Count ) ) {
        return new AutoRoutingSpatialConstraints( _fromEndPoints, _toEndPoints ) ;
      }

      return null ;
    }

    public string LineId => SubRoute.Route.RouteName ;

    public ICommonRoutingCondition Condition { get ; }

    public int RouteCount => _fromEndPoints.Count + _toEndPoints.Count - 1 ;

    public Action<IEnumerable<(IAutoRoutingEndPoint, Vector3d)>> PositionInitialized => SyncTermPositions ;

    private void SyncTermPositions( IEnumerable<(IAutoRoutingEndPoint, Vector3d)> positions )
    {
      foreach ( var (autoRoutingEndPoint, position) in positions ) {
        if ( ! ( autoRoutingEndPoint is EndPointBase endPoint ) ) throw new Exception() ;

        endPoint.SetPosition( position ) ;
      }
    }


    #region Inner classes

    private class AutoRoutingCondition : ICommonRoutingCondition
    {
      private readonly SubRoute _subRoute ;

      public AutoRoutingCondition( Document document, SubRoute subRoute, int priority )
      {
        _subRoute = subRoute ;
        Priority = priority ;
        IsRoutingOnPipeRacks = DocumentMapper.Get( document ).IsRoutingOnPipeRacks( subRoute ) ;
        AllowHorizontalBranches = DocumentMapper.Get( document ).AllowHorizontalBranches( subRoute ) ;
        FixedBopHeight = GetHeight( subRoute.GetReferenceConnector() ) ;
      }

      public bool IsRoutingOnPipeRacks { get ; }
      public LineType Type => _subRoute.Route.ServiceType ;
      public string FluidPhase => _subRoute.Route.FluidPhase ;
      public int Priority { get ; }
      public string GroupName => string.Empty ;
      public LoopType LoopType => _subRoute.Route.LoopType ;

      public bool AllowHorizontalBranches { get ; }
      public double? FixedBopHeight { get ; }

      private static double GetHeight( Connector connector )
      {
        return connector.Origin.Z ;
      }
    }

    private class AutoRoutingSpatialConstraints : IAutoRoutingSpatialConstraints
    {
      public AutoRoutingSpatialConstraints( IReadOnlyCollection<IAutoRoutingEndPoint> fromEndPoints, IReadOnlyCollection<IAutoRoutingEndPoint> toEndPoints )
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