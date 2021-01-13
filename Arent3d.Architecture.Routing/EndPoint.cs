using System ;
using Arent3d.Architecture.Routing.Core ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// An end on auto routing. It may be related to a <see cref="Connector"/> which is not connected.
  /// </summary>
  public class EndPoint : IAutoRoutingEndPoint, IMappedObject<Connector>
  {
    /// <summary>
    /// Returns the related <see cref="Connector"/> to be routed.
    /// </summary>
    public Connector Connector { get ; }

    Connector IMappedObject<Connector>.BaseObject => Connector ;

    /// <summary>
    /// Returns the starting position to be routed.
    /// </summary>
    public Vector3d Position => Connector.Origin.To3d() ;

    /// <summary>
    /// Returns the first pipe direction.
    /// </summary>
    public Vector3d Direction
    {
      get
      {
        var dir = Connector.CoordinateSystem.BasisZ.To3d() ;
        return IsStart ? dir : -dir ;
      }
    }

    /// <summary>
    /// Returns a routing condition object determined from the related <see cref="Connector"/>.
    /// </summary>
    public IRouteCondition PipeCondition { get ; }

    /// <summary>
    /// Returns whether this end point is a from-type end-point.
    /// </summary>
    public bool IsStart { get ; set ; }

    /// <summary>
    /// Always returns 1.
    /// </summary>
    public int Priority => 1 ;

    /// <summary>
    /// Returns this end point's floating type. Now it always returns <see cref="RoutingPointType.OtherNozzle"/> (i.e. non-floated).
    /// </summary>
    public RoutingPointType PointType => RoutingPointType.OtherNozzle ;

    /// <summary>
    /// Not used now. Always returns null.
    /// </summary>
    public IStructureInfo? LinkedRack => null ;

    /// <summary>
    /// Called from <see cref="ObjectMapper{TMapper,TRevitObject,TRoutingObject}"/>
    /// </summary>
    /// <param name="connector">A Revit connector object.</param>
    private EndPoint( Connector connector )
    {
      Connector = connector ;
      PipeCondition = new RouteCondition( connector ) ;
    }

    public void SetPosition( Vector3d position )
    {
      // TODO: set changed position.
    }

    private class RouteCondition : IRouteCondition
    {
      public IPipeDiameter Diameter { get ; }
      public string InsulationType { get ; }
      public double Temperature { get ; }
      public double EffectiveWidth { get ; }
      public IPipeSpec Spec { get ; }

      public RouteCondition( Connector connector )
      {
        Diameter = connector.GetDiameter() ;

        EffectiveWidth = Diameter.Outside ;

        InsulationType = Route.DefaultInsulationType ;
        Temperature = 30 ; // provisional
        Spec = new PipeSpec( connector ) ;
      }

      private class PipeSpec : IPipeSpec
      {
        private readonly Connector _connector ;

        public double GetLongElbowSize( IPipeDiameter diameter )
        {
          return diameter.Outside * 1.5 ; // provisional
        }

        public double Get45ElbowSize( IPipeDiameter diameter )
        {
          return diameter.Outside * 1.5 ; // provisional
        }

        public double GetBranchOffset( IPipeDiameter header, IPipeDiameter branch )
        {
          if ( header.Outside < branch.Outside ) {
            return header.Outside * 1.0 + GetReducerLength( header, branch) ;
          }
          else {
            return header.Outside * 0.5 + branch.Outside * 0.5 ; // provisional
          }
        }

        private double GetReducerLength( IPipeDiameter header, IPipeDiameter branch )
        {
          return 0.0 ;  // TODO
        }

        public double GetWeldMinDistance( IPipeDiameter diameter )
        {
          return 0 ;
        }

        public string Name => _connector.MEPSystem.Name ; // provisional

        public PipeSpec( Connector connector )
        {
          _connector = connector ;
        }
      }
    }
  }
}