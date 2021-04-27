using System ;
using System.Collections.Generic ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Manages relations between pass points and connectors.
  /// </summary>
  public class PassPointConnectorMapper
  {
    private readonly Dictionary<int, (ConnectorId?, ConnectorId?, List<ConnectorId>?)> _passPointConnectors = new() ;

    public void Add( ElementId elementId, bool isFrom, Connector connector )
    {
      var id = elementId.IntegerValue ;

      if ( _passPointConnectors.TryGetValue( id, out var tuple ) ) {
        if ( isFrom ) {
          if ( null != tuple.Item1 ) throw new InvalidOperationException() ;
          _passPointConnectors[ id ] = ( new ConnectorId( connector ), tuple.Item2, tuple.Item3 ) ;
        }
        else {
          if ( null != tuple.Item2 ) throw new InvalidOperationException() ;
          _passPointConnectors[ id ] = ( tuple.Item1, new ConnectorId( connector ), tuple.Item3 ) ;
          _passPointConnectors[ id ] = ( tuple.Item1, new ConnectorId( connector ), tuple.Item3 ) ;
        }
      }
      else {
        if ( isFrom ) {
          _passPointConnectors.Add( id, ( new ConnectorId( connector ), null, tuple.Item3 ) ) ;
        }
        else {
          _passPointConnectors.Add( id, ( null, new ConnectorId( connector ), tuple.Item3 ) ) ;
        }
      }
    }

    public void AddBranch( ElementId elementId, Connector connector )
    {
      var id = elementId.IntegerValue ;

      if ( _passPointConnectors.TryGetValue( id, out var tuple ) ) {
        if ( null == tuple.Item3 ) {
          tuple.Item3 = new List<ConnectorId>() ;
          _passPointConnectors[ id ] = tuple ;
        }
      }
      else {
        tuple = ( null, null, new List<ConnectorId>() ) ;
        _passPointConnectors.Add( id, tuple ) ;
      }

      tuple.Item3.Add( new ConnectorId( connector ) ) ;
    }

    public void Merge( PassPointConnectorMapper another )
    {
      foreach ( var (id, (connector1, connector2, list)) in another._passPointConnectors ) {
        if ( _passPointConnectors.TryGetValue( id, out var tuple ) ) {
          if ( null != tuple.Item1 && null != connector1 ) throw new InvalidOperationException() ;
          if ( null != tuple.Item2 && null != connector2 ) throw new InvalidOperationException() ;

          _passPointConnectors[ id ] = ( tuple.Item1 ?? connector1, tuple.Item2 ?? connector2, MergeList( list, tuple.Item3 ) ) ;
        }
        else {
          _passPointConnectors.Add( id, ( connector1, connector2, list ) ) ;
        }
      }
    }

    private static List<ConnectorId>? MergeList( List<ConnectorId>? list1, List<ConnectorId>? list2 )
    {
      if ( null != list1 && null != list2 ) {
        var result = new List<ConnectorId>() ;
        result.AddRange( list1 ) ;
        result.AddRange( list2 ) ;
        return result ;
      }

      return list1 ?? list2 ;
    }

    public IEnumerable<(int, (Connector, Connector, IReadOnlyList<Connector>?))> GetPassPointConnections( Document document )
    {
      foreach ( var (key, (cid1, cid2, others)) in _passPointConnectors ) {
        if ( cid1?.GetConnector( document ) is not { } con1 ) continue ;
        if ( cid2?.GetConnector( document ) is not { } con2 ) continue ;

        yield return ( key, ( con1, con2, ( null == others ? null : ConnectorId.ToConnectorList( document, others ) ) ) ) ;
      }
    }
  }
}