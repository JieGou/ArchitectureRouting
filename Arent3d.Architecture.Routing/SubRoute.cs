using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoint ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public class SubRoute
  {
    public Route Route { get ; }
    
    public int SubRouteIndex { get ; }

    private readonly List<IEndPointIndicator> _fromEndPointIndicators = new() ;
    private readonly List<IEndPointIndicator> _toEndPointIndicators = new() ;

    public IEnumerable<IEndPointIndicator> FromEndPointIndicators => _fromEndPointIndicators ;
    public IEnumerable<IEndPointIndicator> ToEndPointIndicators => _toEndPointIndicators ;

    public IEnumerable<IEndPointIndicator> AllEndPointIndicators => _fromEndPointIndicators.Concat( _toEndPointIndicators ) ;

    public IEnumerable<EndPointBase> GetFromEndPoints( Document document )
    {
      return _fromEndPointIndicators.Select( ep => ep.GetAutoRoutingEndPoint( document, this, true ) ).NonNull() ;
    }

    public IEnumerable<EndPointBase> GetToEndPoints( Document document )
    {
      return _toEndPointIndicators.Select( ep => ep.GetAutoRoutingEndPoint( document, this, false ) ).NonNull() ;
    }

    internal SubRoute( Route route, int index )
    {
      Route = route ;
      SubRouteIndex = index ;
    }

    internal void AddFrom( IEndPointIndicator from )
    {
      _fromEndPointIndicators.Add( from ) ;
    }
    internal void AddTo( IEndPointIndicator to )
    {
      _toEndPointIndicators.Add( to ) ;
    }

    internal void Merge( SubRoute another )
    {
      _fromEndPointIndicators.AddRange( another._fromEndPointIndicators ) ;
      _toEndPointIndicators.AddRange( another._toEndPointIndicators ) ;
    }

    /// <summary>
    /// Returns a representative connector of this sub route if exists.
    /// </summary>
    /// <returns>Connector.</returns>
    internal Connector? GetReferenceConnectorInSubRoute()
    {
      return _fromEndPointIndicators.Concat( _toEndPointIndicators ).OfType<ConnectorIndicator>().Select( ind => ind.GetConnector( Route.Document ) ).NonNull().FirstOrDefault() ;
    }

    /// <summary>
    /// Returns a representative connector whose parameters are used for MEP system creation.
    /// </summary>
    /// <returns>Connector.</returns>
    public Connector GetReferenceConnector()
    {
      return GetReferenceConnectorInSubRoute() ?? Route.GetReferenceConnector() ;
    }
  }
}