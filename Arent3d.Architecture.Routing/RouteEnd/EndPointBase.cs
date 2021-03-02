using Arent3d.Routing ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  /// <summary>
  /// An end on auto routing.
  /// </summary>
  public abstract class EndPointBase
  {
    /// <summary>
    /// Owner route of the end point.
    /// </summary>
    public Route OwnerRoute { get ; }

    /// <summary>
    /// Returns the indicator for this end point.
    /// </summary>
    public abstract IEndPointIndicator EndPointIndicator { get ; }

    /// <summary>
    /// Returns the representative connector whose parameters are used for MEP system creation.
    /// </summary>
    public Connector ReferenceConnector { get ; }

    /// <summary>
    /// Returns the starting position to be routed.
    /// </summary>
    public abstract Vector3d Position { get ; }

    /// <summary>
    /// Returns the flow vector.
    /// </summary>
    public abstract Vector3d GetDirection( bool isFrom ) ;

    /// <summary>
    /// Returns the end point's diameter if exists.
    /// </summary>
    /// <returns>Diameter.</returns>
    public abstract double? GetDiameter() ;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="ownerRoute">Owner route.</param>
    /// <param name="connector">A Revit connector object.</param>
    protected EndPointBase( Route ownerRoute, Connector connector )
    {
      OwnerRoute = ownerRoute ;
      ReferenceConnector = connector ;
    }
  }

  internal static class EndPointExtensions
  {
    public static Vector3d ForEndPointType( this Vector3d direction, bool isFrom ) => isFrom ? direction : -direction ;
    

    public static EndPointBase? GetEndPoint( this IAutoRoutingEndPoint endPoint )
    {
      return endPoint switch
      {
        AutoRoutingEndPoint ep => ep.EndPoint,
        IPseudoEndPoint pep => pep.Source.GetEndPoint(),
        _ => null,
      } ;
    }
  }
}