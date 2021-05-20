using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Routing ;
using Arent3d.Routing.Conditions ;
using Arent3d.Utility ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Wrapper class for routing-core of <see cref="IAutoRoutingEndPoint"/>.
  /// </summary>
  public class AutoRoutingEndPoint : IAutoRoutingEndPoint
  {
    public IEndPoint EndPoint { get ; }

    private readonly double _minimumStraightLength ;
    private readonly double _angleToleranceRadian ;

    /// <summary>
    /// Wrap end point as an instance of <see cref="IAutoRoutingEndPoint"/>.
    /// </summary>
    /// <param name="endPoint">Base end point.</param>
    /// <param name="isFrom">True if this end point represents a from-side end point.</param>
    /// <param name="priority">Priority (can be duplicated between end points in an <see cref="AutoRoutingTarget"/>).</param>
    /// <param name="routeMepSystem">A <see cref="RouteMEPSystem"/> object this end point belongs to.</param>
    /// <param name="edgeDiameter">Edge diameter.</param>
    internal AutoRoutingEndPoint( IEndPoint endPoint, bool isFrom, int priority, RouteMEPSystem routeMepSystem, double edgeDiameter, ProcessConstraint avoidType )
    {
      EndPoint = endPoint ;
      IsStart = isFrom ;
      Priority = priority ;
      Depth = priority ;
      _minimumStraightLength = routeMepSystem.GetReducerLength( endPoint.GetDiameter() ?? -1, edgeDiameter ) + EndPoint.GetMinimumStraightLength( routeMepSystem, edgeDiameter, IsStart ) ;
      _angleToleranceRadian = routeMepSystem.AngleTolerance ;

      PipeCondition = new RouteCondition( routeMepSystem, endPoint, edgeDiameter, avoidType ) ;
    }

    public Vector3d Position => EndPoint.RoutingStartPosition.To3dRaw() + _minimumStraightLength * Direction.ForEndPointType( IsStart ) ;

    public Vector3d Direction => Sanitize( EndPoint.GetRoutingDirection( IsStart ).To3dRaw() ) ;

    private static readonly Vector3d[] SanitizationDirections =
    {
      new Vector3d( +1, 0, 0 ), new Vector3d( -1, 0, 0 ),
      new Vector3d( 0, +1, 0 ), new Vector3d( 0, -1, 0 ),
      new Vector3d( 0, 0, +1 ), new Vector3d( 0, 0, -1 ),
    } ;
    private Vector3d Sanitize( Vector3d vec )
    {
      foreach ( var dir in SanitizationDirections ) {
        double dot = Vector3d.Dot( vec, dir ), cross = Vector3d.Cross( vec, dir ).magnitude ;
        if ( Math.Atan2( cross, dot ) < _angleToleranceRadian ) return dir ;
      }

      return vec ;
    }

    /// <summary>
    /// Returns a routing condition object determined from the related connector.
    /// </summary>
    public IRouteCondition PipeCondition { get ; }

    /// <summary>
    /// Returns whether this end point is from-side end point.
    /// </summary>
    public bool IsStart { get ; }

    /// <summary>
    /// Returns the priority. <see cref="Priority"/> is similar to <see cref="Depth"/>, but can be duplicated between end points in an <see cref="AutoRoutingTarget"/>.
    /// </summary>
    public int Priority { get ; }

    /// <summary>
    /// Returns the priority. <see cref="Depth"/> is similar to <see cref="Priority"/>, but cannot be duplicated between end points in an <see cref="AutoRoutingTarget"/>.
    /// </summary>
    public int Depth { get ; private set ; }

    /// <summary>
    /// Returns this end point's floating type. Now it always returns <see cref="RoutingPointType.OtherNozzle"/> (i.e. non-floated).
    /// </summary>
    public RoutingPointType PointType => RoutingPointType.OtherNozzle ;

    /// <summary>
    /// Not used now. Always returns null.
    /// </summary>
    public IStructureInfo? LinkedRack => null ;

    /// <summary>
    /// Apply depths from priorities in an <see cref="AutoRoutingTarget"/>.
    /// </summary>
    /// <param name="fromList">From-side end points in an <see cref="AutoRoutingTarget"/>.</param>
    /// <param name="toList">To-side end points in an <see cref="AutoRoutingTarget"/>.</param>
    public static void ApplyDepths( IReadOnlyCollection<AutoRoutingEndPoint> fromList, IReadOnlyCollection<AutoRoutingEndPoint> toList )
    {
      if ( 0 == fromList.Count || 0 == toList.Count ) return ;

      if ( 1 == fromList.Count && 1 == toList.Count ) {
        // Can ignore priority.
        fromList.First().Depth = 0 ;
        toList.First().Depth = 0 ;
        return ;
      }

      var fromPriorities = ByPriority( fromList ) ;
      var toPriorities = ByPriority( toList ) ;
      
      {
        // If from-list or to-list has two `Priority = 0' end points, make certain each list to have only one `Priority = 0' end point.
        var (fromFirstPriority, fromFirstPriorityList) = fromPriorities.First() ;
        var (toFirstPriority, toFirstPriorityList) = toPriorities.First() ;
        if ( 1 < fromFirstPriorityList.Count ) {
          // first element is before others.
          var firstEp = fromFirstPriorityList[ 0 ] ;
          fromPriorities.Add( Math.Min( fromFirstPriority, toFirstPriority ) - 1, new List<AutoRoutingEndPoint> { firstEp } ) ;
          fromFirstPriorityList.RemoveAt( 0 ) ;
        }

        if ( 1 < toFirstPriorityList.Count ) {
          // first element is before others.
          var firstEp = toFirstPriorityList[ 0 ] ;
          toPriorities.Add( Math.Min( fromFirstPriority, toFirstPriority ) - 1, new List<AutoRoutingEndPoint> { firstEp } ) ;
          toFirstPriorityList.RemoveAt( 0 ) ;
        }
      }

      {
        // If the priority first end point is different from from-list and to-list, make same.
        var (fromFirstPriority, fromFirstPriorityList) = fromPriorities.First() ;
        var (toFirstPriority, toFirstPriorityList) = toPriorities.First() ;
        if ( fromFirstPriority < toFirstPriority ) {
          toPriorities.RemoveAt( 0 ) ;
          toPriorities.Add( fromFirstPriority, toFirstPriorityList ) ;
        }
        else if ( fromFirstPriority > toFirstPriority ) {
          toPriorities.RemoveAt( 0 ) ;
          toPriorities.Add( toFirstPriority, fromFirstPriorityList ) ;
        }
      }

      // Reorder by priority
      CombineLists( fromPriorities, toPriorities ).ForEach( ( endPoints, i ) => endPoints.ForEach( ep => ep.Depth = i ) ) ;
    }

    private static SortedList<int, List<AutoRoutingEndPoint>> ByPriority( IEnumerable<AutoRoutingEndPoint> list )
    {
      var result = new SortedList<int, List<AutoRoutingEndPoint>>() ;

      foreach ( var ep in list ) {
        if ( false == result.TryGetValue( ep.Priority, out var listInPriority ) ) {
          listInPriority = new List<AutoRoutingEndPoint>() ;
          result.Add( ep.Priority, listInPriority ) ;
        }
        listInPriority.Add( ep ) ;
      }

      return result ;
    }

    private static IEnumerable<IEnumerable<AutoRoutingEndPoint>> CombineLists( SortedList<int, List<AutoRoutingEndPoint>> list1, SortedList<int, List<AutoRoutingEndPoint>> list2 )
    {
      using var enu1 = list1.GetEnumerator() ;
      using var enu2 = list2.GetEnumerator() ;

      bool hasValue1 = enu1.MoveNext(), hasValue2 = enu2.MoveNext() ;
      while ( hasValue1 || hasValue2 ) {
        if ( ! hasValue2 || ( hasValue1 && ( enu1.Current.Key < enu2.Current.Key ) ) ) {
          yield return enu1.Current.Value ;
          hasValue1 = enu1.MoveNext() ;
        }
        else if ( ! hasValue1 || ( hasValue2 && ( enu2.Current.Key < enu1.Current.Key ) ) ) {
          yield return enu2.Current.Value ;
          hasValue2 = enu2.MoveNext() ;
        }
        else {
          yield return enu1.Current.Value.Concat( enu2.Current.Value ) ;
          hasValue1 = enu1.MoveNext() ;
          hasValue2 = enu2.MoveNext() ;
        }
      }
    }


    private class RouteCondition : IRouteCondition
    {
      private const string DefaultFluidPhase = "None" ;

      public IPipeDiameter Diameter { get ; }
      public double DiameterPipeAndInsulation => Diameter.Outside ;
      public double DiameterFlangeAndInsulation => Diameter.Outside ; // provisional
      public IPipeSpec Spec { get ; }
      public ProcessConstraint ProcessConstraint { get ; private set; }
      public string FluidPhase => DefaultFluidPhase ;

      public RouteCondition( RouteMEPSystem routeMepSystem, IEndPoint endPoint, double diameter, ProcessConstraint avoidType )
      {
        Diameter = diameter.DiameterValueToPipeDiameter() ;

        Spec = new MEPSystemPipeSpec( routeMepSystem ) ;

        ProcessConstraint = avoidType ;
      }
    }
  }
}