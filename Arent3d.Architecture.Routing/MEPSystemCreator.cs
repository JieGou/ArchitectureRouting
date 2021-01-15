using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Routing ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Creates a mechanical system from auto routing results.
  /// </summary>
  public class MEPSystemCreator
  {
    private readonly Document _document ;
    private readonly RouteVertexToConnectorMapper _connectorMapper = new() ;

    private readonly Level _level ;

    private readonly MEPSystemType _systemType ;

    private readonly List<Connector> _badConnectors = new() ;

    public MEPSystemCreator( Document document, AutoRoutingTarget autoRoutingTarget )
    {
      _document = document ;
      _level = CreateLevel( document ) ;
      _systemType = GetSystemType( document, autoRoutingTarget ) ;
    }

    private static Level CreateLevel( Document document )
    {
      return Level.Create( document, 0.0 ) ;
    }

    private static MEPSystemType GetSystemType( Document document, AutoRoutingTarget autoRoutingTarget )
    {
      var systemClassification = GetSystemClassification( autoRoutingTarget.EndPoints.First().Connector ) ;
      foreach ( var type in document.GetAllElements<MEPSystemType>() ) {
        if ( type.SystemClassification == systemClassification ) {
          return type ;
        }
      }

      throw new Exception() ;
    }

    private static MEPSystemClassification GetSystemClassification( Connector connector )
    {
      return connector.Domain switch
      {
        Domain.DomainPiping => GetSystemClassification( connector.PipeSystemType ),
        Domain.DomainHvac => GetSystemClassification( connector.DuctSystemType ),
        Domain.DomainElectrical => GetSystemClassification( connector.ElectricalSystemType ),
        Domain.DomainCableTrayConduit => GetSystemClassification( connector.ElectricalSystemType ),
        _ => null,
      } ?? throw new KeyNotFoundException() ;
    }

    private static MEPSystemClassification? GetSystemClassification<T>( T systemType ) where T : Enum
    {
      try {
        if ( Enum.TryParse( systemType.ToString(), out MEPSystemClassification result ) ) {
          return result ;
        }

        return null ;
      }
      catch {
        return null ;
      }
    }

    public IReadOnlyCollection<Connector> GetBadConnectors() => _badConnectors ;

    /// <summary>
    /// Registers related end route vertex and connector for an end point.
    /// </summary>
    /// <param name="routeVertex">A route vertex generated from an end point.</param>
    /// <exception cref="ArgumentException"><see cref="routeVertex"/> is not generated from an end point.</exception>
    public void RegisterEndPointConnector( IRouteVertex routeVertex )
    {
      _connectorMapper.Add( routeVertex, GetSourceConnector( routeVertex.LineInfo ) ) ;
    }

    private static Connector GetSourceConnector( IAutoRoutingEndPoint endPoint )
    {
      return endPoint switch
      {
        EndPoint ep => ep.Connector,
        IPseudoEndPoint pep => GetSourceConnector( pep.Source ),
        _ => throw new ArgumentException()
      } ;
    }

    /// <summary>
    /// Creates a duct from a route edge.
    /// </summary>
    /// <param name="routeEdge">A route edge.</param>
    /// <returns>Newly created duct.</returns>
    public Element CreateEdgeElement( IRouteEdge routeEdge )
    {
      var startPos = _connectorMapper.GetNewConnectorPosition( routeEdge.Start, routeEdge.End ).ToXYZ() ;
      var endPos = _connectorMapper.GetNewConnectorPosition( routeEdge.End, routeEdge.Start ).ToXYZ() ;

      var baseConnector = ( routeEdge.LineInfo as EndPoint )?.Connector ;
      if ( null == baseConnector ) throw new InvalidOperationException() ;

      var element = baseConnector.Domain switch
      {
        Domain.DomainHvac => CreateDuct( startPos, endPos ),
        Domain.DomainPiping => CreatePipe( startPos, endPos ),
        _ => throw new InvalidOperationException(),
      } ;

      var manager = element.GetConnectorManager() ?? throw new InvalidOperationException() ;

      var startConnector = GetConnector( manager, startPos ) ;
      var endConnector = GetConnector( manager, endPos ) ;
      startConnector.SetDiameter( routeEdge.Start.PipeDiameter ) ;
      endConnector.SetDiameter( routeEdge.End.PipeDiameter ) ;

      _connectorMapper.Add( routeEdge.Start, startConnector ) ;
      _connectorMapper.Add( routeEdge.End, endConnector ) ;

      return element ;
    }

    private Element CreateDuct( XYZ startPos, XYZ endPos )
    {
      var ductTypeId = _document.GetDefaultElementTypeId( ElementTypeGroup.DuctType ) ;
      return Duct.Create( _document, _systemType.Id, ductTypeId, _level.Id, startPos, endPos ) ;
    }
    private Element CreatePipe( XYZ startPos, XYZ endPos )
    {
      var pipeTypeId = _document.GetDefaultElementTypeId( ElementTypeGroup.PipeType ) ;
      return Pipe.Create( _document, _systemType.Id, pipeTypeId, _level.Id, startPos, endPos ) ;
    }

    private static Connector GetConnector( ConnectorManager connectorManager, XYZ position )
    {
      foreach ( Connector conn in connectorManager.Connectors ) {
        if ( conn.ConnectorType == ConnectorType.Logical ) continue ;
        if ( conn.Origin.IsAlmostEqualTo( position ) ) return conn ;
      }

      throw new InvalidOperationException() ;
    }

    /// <summary>
    /// Connect all connectors related to each route vertex.
    /// </summary>
    public void ConnectAllVertices()
    {
      foreach ( var connectors in _connectorMapper ) {
        ConnectConnectors( connectors ) ;
      }
    }

    /// <summary>
    /// Connect all connectors.
    /// </summary>
    /// <param name="connectors">Connectors to be connected</param>
    private void ConnectConnectors( IReadOnlyList<Connector> connectors )
    {
      switch ( connectors.Count ) {
        case 1 : return ;
        case 2 :
          ConnectTwoConnectors( connectors[ 0 ], connectors[ 1 ] ) ;
          break ;
        case 3 :
          ConnectThreeConnectors( connectors[ 0 ], connectors[ 1 ], connectors[ 2 ] ) ;
          break ;
        case 4 :
          ConnectFourConnectors( connectors[ 0 ], connectors[ 1 ], connectors[ 2 ], connectors[ 3 ] ) ;
          break ;
        default : throw new InvalidOperationException() ;
      }
    }

    /// <summary>
    /// Connect two connectors. Elbow is inserted if needed.
    /// </summary>
    /// <param name="connector1"></param>
    /// <param name="connector2"></param>
    private void ConnectTwoConnectors( Connector connector1, Connector connector2 )
    {
      connector1.ConnectTo( connector2 ) ;

      var dir1 = connector1.CoordinateSystem.BasisZ.To3d() ;
      var dir2 = connector2.CoordinateSystem.BasisZ.To3d() ;

      if ( 0.9 < Math.Abs( Vector3d.Dot( dir1, dir2 ) ) ) {
        // Connect directly(-1) or bad connection(+1)
      }
      else {
        // Orthogonal
        if ( CanCreateElbow( connector1, connector2 ) ) {
          var family = _document.Create.NewElbowFitting( connector1, connector2 ) ;
          EraseZeroLengthDuct( family ) ;
        }
        else {
          AddBadConnector( connector1 ) ;
        }
      }
    }

    /// <summary>
    /// Connect three connectors. Tee is inserted.
    /// </summary>
    /// <param name="connector1"></param>
    /// <param name="connector2"></param>
    /// <param name="connector3"></param>
    private void ConnectThreeConnectors( Connector connector1, Connector connector2, Connector connector3 )
    {
      connector1.ConnectTo( connector2 ) ;
      connector1.ConnectTo( connector3 ) ;
      connector2.ConnectTo( connector3 ) ;

      if ( CanCreateTee( connector1, connector2, connector3 ) ) {
        var family = _document.Create.NewTeeFitting( connector1, connector2, connector3 ) ;
        EraseZeroLengthDuct( family ) ;
      }
      else {
        AddBadConnector( connector1 ) ;
      }
    }

    /// <summary>
    /// Connect four connectors. Cross is inserted.
    /// </summary>
    /// <param name="connector1"></param>
    /// <param name="connector2"></param>
    /// <param name="connector3"></param>
    /// <param name="connector4"></param>
    private void ConnectFourConnectors( Connector connector1, Connector connector2, Connector connector3, Connector connector4 )
    {
      connector1.ConnectTo( connector2 ) ;
      connector1.ConnectTo( connector3 ) ;
      connector1.ConnectTo( connector4 ) ;
      connector2.ConnectTo( connector3 ) ;
      connector2.ConnectTo( connector4 ) ;
      connector3.ConnectTo( connector4 ) ;

      if ( CanCreateCross( connector1, connector2, connector3, connector4 ) ) {
        var family = _document.Create.NewCrossFitting( connector1, connector2, connector3, connector4 ) ;
        EraseZeroLengthDuct( family ) ;
      }
      else {
        AddBadConnector( connector1 ) ;
      }
    }

    private bool CanCreateElbow( Connector connector1, Connector connector2 )
    {
      // TODO

      return true ;
    }

    private bool CanCreateTee( Connector connector1, Connector connector2, Connector connector3 )
    {
      // TODO

      return true ;
    }

    private bool CanCreateCross( Connector connector1, Connector connector2, Connector connector3, Connector connector4 )
    {
      // TODO

      return true ;
    }

    private void EraseZeroLengthDuct( FamilyInstance family )
    {
      foreach ( Connector connector in family.MEPModel.ConnectorManager.Connectors ) {
        EraseZeroLengthDuct( connector ) ;
      }
    }

    private void EraseZeroLengthDuct( Connector connector )
    {
      var ductConn1 = GetConnectingDuctConnector( connector ) ;
      if ( ductConn1?.Owner is not Duct duct ) return ;

      var ductConn2 = GetAnotherDuctConnector( ductConn1 ) ;
      if ( null == ductConn2 ) return ;

      if ( false == ductConn1.Origin.IsAlmostEqualTo( ductConn2.Origin ) ) return ;

      // TODO: erase duct
    }

    private static (Vector3d From, Vector3d To) GetLine( Connector connector )
    {
      var another = GetAnotherDuctConnector( connector ) ?? throw new InvalidOperationException() ;
      return ( From: connector.Origin.To3d(), To: another.Origin.To3d() ) ;
    }

    private static Connector? GetAnotherDuctConnector( Connector connector )
    {
      return connector.ConnectorManager.Connectors.OfType<Connector>().Where( c => ! IsSameConnector( c, connector ) ).UniqueOrDefault() ;
    }

    private static Connector? GetConnectingDuctConnector( Connector connector )
    {
      return connector.AllRefs.OfType<Connector>().Where( c => ! IsSameConnector( c, connector ) && c.Owner is Duct ).UniqueOrDefault() ;
    }

    private static bool IsSameConnector( Connector connector1, Connector connector2 )
    {
      return ( connector1.Owner.Id == connector2.Owner.Id ) && ( connector1.Id == connector2.Id ) ;
    }

    private void AddBadConnector( Connector connector )
    {
      _badConnectors.Add( connector ) ;
    }
  }
}