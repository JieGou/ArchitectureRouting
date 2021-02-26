using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoint ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Manages relations between pass points and connectors.
  /// </summary>
  public class PassPointConnectorMapper : IEnumerable<KeyValuePair<int, (Connector, Connector, List<Connector>?)>>
  {
    private readonly Dictionary<int, (Connector?, Connector?, List<Connector>?)> _passPointConnectors = new() ;

    public void Add( ElementId elementId, PassPointEndSide sideType, Connector connector )
    {
      var id = elementId.IntegerValue ;

      if ( _passPointConnectors.TryGetValue( id, out var tuple ) ) {
        if ( sideType == PassPointEndSide.Forward ) {
          if ( null != tuple.Item1 ) throw new InvalidOperationException() ;
          _passPointConnectors[ id ] = ( connector, tuple.Item2, tuple.Item3 ) ;
        }
        else {
          if ( null != tuple.Item2 ) throw new InvalidOperationException() ;
          _passPointConnectors[ id ] = ( tuple.Item1, connector, tuple.Item3 ) ;
        }
      }
      else {
        if ( sideType == PassPointEndSide.Forward ) {
          _passPointConnectors.Add( id, ( connector, null, tuple.Item3 ) ) ;
        }
        else {
          _passPointConnectors.Add( id, ( null, connector, tuple.Item3 ) ) ;
        }
      }
    }

    public void AddBranch( ElementId elementId, Connector connector )
    {
      var id = elementId.IntegerValue ;

      if ( _passPointConnectors.TryGetValue( id, out var tuple ) ) {
        if ( null == tuple.Item3 ) {
          tuple.Item3 = new List<Connector>() ;
          _passPointConnectors[ id ] = tuple ;
        }
      }
      else {
        tuple = ( null, null, new List<Connector>() ) ;
        _passPointConnectors.Add( id, tuple ) ;
      }

      tuple.Item3.Add( connector ) ;
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

    private static List<Connector>? MergeList( List<Connector>? list1, List<Connector>? list2 )
    {
      if ( null != list1 && null != list2 ) {
        var result = new List<Connector>() ;
        result.AddRange( list1 ) ;
        result.AddRange( list2 ) ;
        return result ;
      }

      return list1 ?? list2 ;
    }

    public IEnumerator<KeyValuePair<int, (Connector, Connector, List<Connector>?)>> GetEnumerator()
    {
      foreach ( var (key, (con1, con2, others)) in _passPointConnectors ) {
        if ( null == con1 || null == con2 ) continue ;

        yield return new KeyValuePair<int, (Connector, Connector, List<Connector>?)>( key, ( con1, con2, others ) ) ;
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator() ;
    }
  }
}