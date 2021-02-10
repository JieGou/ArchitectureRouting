using System ;
using System.Collections ;
using System.Collections.Generic ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Manages relations between pass points and connectors.
  /// </summary>
  public class PassPointConnectorMapper : IEnumerable<KeyValuePair<int, (Connector, Connector)>>
  {
    private readonly Dictionary<int, (Connector?, Connector?)> _passPointConnectors = new() ;

    public void Add( ElementId elementId, PassPointEndSide sideType, Connector connector )
    {
      var id = elementId.IntegerValue ;

      if ( _passPointConnectors.TryGetValue( id, out var tuple ) ) {
        if ( sideType == PassPointEndSide.Forward ) {
          if ( null != tuple.Item1 ) throw new InvalidOperationException() ;
          _passPointConnectors[ id ] = ( connector, tuple.Item2 ) ;
        }
        else {
          if ( null != tuple.Item2 ) throw new InvalidOperationException() ;
          _passPointConnectors[ id ] = ( tuple.Item1, connector ) ;
        }
      }
      else {
        if ( sideType == PassPointEndSide.Forward ) {
          _passPointConnectors.Add( id, ( connector, null ) ) ;
        }
        else {
          _passPointConnectors.Add( id, ( null, connector ) ) ;
        }
      }
    }

    public void Merge( PassPointConnectorMapper another )
    {
      foreach ( var (id, (connector1, connector2)) in another._passPointConnectors ) {
        if ( _passPointConnectors.TryGetValue( id, out var tuple ) ) {
          if ( null != tuple.Item1 && null != connector1 ) throw new InvalidOperationException() ;
          if ( null != tuple.Item2 && null != connector2 ) throw new InvalidOperationException() ;

          _passPointConnectors[ id ] = ( tuple.Item1 ?? connector1, tuple.Item2 ?? connector2 ) ;
        }
        else {
          _passPointConnectors.Add( id, ( connector1, connector2 ) ) ;
        }
      }
    }

    public IEnumerator<KeyValuePair<int, (Connector, Connector)>> GetEnumerator()
    {
      return _passPointConnectors.GetEnumerator() ;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator() ;
    }
  }
}