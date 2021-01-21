using System ;
using Arent3d.Routing ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// An end on auto routing.
  /// </summary>
  public abstract class EndPoint : IAutoRoutingEndPoint
  {
    /// <summary>
    /// Owner route of the end point.
    /// </summary>
    public Route OwnerRoute { get ; }
    
    /// <summary>
    /// Returns the representative connector whose parameters are used for MEP system creation.
    /// </summary>
    public Connector ReferenceConnector { get ; }

    /// <summary>
    /// Returns the starting position to be routed.
    /// </summary>
    public abstract Vector3d Position { get ; }

    /// <summary>
    /// Returns the first pipe direction.
    /// </summary>
    public abstract Vector3d Direction { get ; }

    /// <summary>
    /// Returns a routing condition object determined from the related <see cref="ReferenceConnector"/>.
    /// </summary>
    public IRouteCondition PipeCondition { get ; }

    /// <summary>
    /// Returns whether this end point is a from-type end-point.
    /// </summary>
    public bool IsStart { get ; }

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
    /// Constructor.
    /// </summary>
    /// <param name="ownerRoute">Owner route.</param>
    /// <param name="connector">A Revit connector object.</param>
    /// <param name="isStart">True if this end point represents a start end point.</param>
    protected EndPoint( Route ownerRoute, Connector connector, bool isStart )
    {
      OwnerRoute = ownerRoute ;
      ReferenceConnector = connector ;
      IsStart = isStart ;
      PipeCondition = new RouteCondition( this ) ;
    }

    public virtual void SetPosition( Vector3d position )
    {
      // do nothing because this end point is not floating.
    }

    private class RouteCondition : IRouteCondition
    {
      public IPipeDiameter Diameter { get ; }
      public string InsulationType { get ; }
      public double Temperature { get ; }
      public double DiameterPipeAndInsulation => Diameter.Outside ;
      public double DiameterFlangeAndInsulation => Diameter.Outside ; // provisional
      public IPipeSpec Spec { get ; }

      public RouteCondition( EndPoint endPoint )
      {
        Diameter = endPoint.ReferenceConnector.GetDiameter() ;

        InsulationType = Route.DefaultInsulationType ;
        Temperature = endPoint.OwnerRoute.Temperature ;
        Spec = new PipeSpec( endPoint ) ;
      }

      private class PipeSpec : IPipeSpec
      {
        private readonly EndPoint _endPoint ;

        public double GetLongElbowSize( IPipeDiameter diameter )
        {
          return diameter.Outside * 1.5 ; // provisional
        }

        public double Get45ElbowSize( IPipeDiameter diameter )
        {
          return diameter.Outside * 1.5 ; // provisional
        }

        public double GetTeeBranchLength( IPipeDiameter header, IPipeDiameter branch )
        {
          if ( header.Outside < branch.Outside ) {
            return header.Outside * 1.0 + GetReducerLength( header, branch) ;
          }
          else {
            return header.Outside * 0.5 + branch.Outside * 0.5 ; // provisional
          }
        }

        public double GetTeeHeaderLength( IPipeDiameter header, IPipeDiameter branch )
        {
          if ( header.Outside < branch.Outside ) {
            return header.Outside * 1.0 ; // provisional
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

        public string Name => _endPoint.ReferenceConnector.MEPSystem.Name ; // provisional

        public PipeSpec( EndPoint endPoint )
        {
          _endPoint = endPoint ;
        }
      }
    }
  }
}