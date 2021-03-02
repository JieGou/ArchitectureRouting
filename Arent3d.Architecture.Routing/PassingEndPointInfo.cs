using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Arent3d.Routing ;

namespace Arent3d.Architecture.Routing
{
  public class PassingEndPointInfo
  {
    public static IReadOnlyDictionary<IRouteEdge, PassingEndPointInfo> CollectPassingEndPointInfo( AutoRoutingResult result )
    {
      var dic = new Dictionary<IRouteEdge, PassingEndPointInfo>() ;

      var linkInfo = new Dictionary<IRouteVertex, (List<IRouteEdge> Enter, List<IRouteEdge> Exit)>() ;
      foreach ( var edge in result.RouteEdges ) {
        AddLinkInfo( linkInfo, edge.Start, edge, true ) ;
        AddLinkInfo( linkInfo, edge.End, edge, false ) ;
      }

      foreach ( var edge in result.RouteEdges ) {
        SeekEndPoints( dic, linkInfo, edge, true ) ;
        SeekEndPoints( dic, linkInfo, edge, false ) ;
      }

      return dic ;
    }

    private static void AddLinkInfo( Dictionary<IRouteVertex, (List<IRouteEdge> Enter, List<IRouteEdge> Exit)> linkInfo, IRouteVertex vertex, IRouteEdge edge, bool isExit )
    {
      if ( false == linkInfo.TryGetValue( vertex, out var tuple ) ) {
        tuple = ( new List<IRouteEdge>(), new List<IRouteEdge>() ) ;
        linkInfo.Add( vertex, tuple ) ;
      }

      if ( isExit ) {
        tuple.Exit.Add( edge ) ;
      }
      else {
        tuple.Enter.Add( edge ) ;
      }
    }

    private static IEnumerable<IEndPointIndicator> SeekEndPoints( Dictionary<IRouteEdge, PassingEndPointInfo> dic, IReadOnlyDictionary<IRouteVertex, (List<IRouteEdge> Enter, List<IRouteEdge> Exit)> linkInfo, IRouteEdge edge, bool seekFrom )
    {
      if ( false == dic.TryGetValue( edge, out var fromTo ) ) {
        fromTo = new PassingEndPointInfo() ;
        dic.Add( edge, fromTo ) ;
      }

      if ( seekFrom ) {
        if ( 0 == fromTo._fromEndPoints.Count ) {
          if ( edge.Start is TerminalPoint tp ) {
            fromTo.RegisterFrom( tp.LineInfo.GetEndPoint() ) ;
          }
          fromTo.RegisterFrom( linkInfo[ edge.Start ].Enter.SelectMany( e => SeekEndPoints( dic, linkInfo, e, true ) ) ) ;
        }

        return fromTo._fromEndPoints ;
      }
      else {
        if ( 0 == fromTo._toEndPoints.Count ) {
          if ( edge.End is TerminalPoint tp ) {
            fromTo.RegisterTo( tp.LineInfo.GetEndPoint() ) ;
          }
          fromTo.RegisterTo( linkInfo[ edge.End ].Exit.SelectMany( e => SeekEndPoints( dic, linkInfo, e, false ) ) ) ;
        }

        return fromTo._toEndPoints ;
      }
    }

    private void RegisterFrom( EndPointBase? endPoint )
    {
      if ( null != endPoint ) {
        _fromEndPoints.Add( endPoint.EndPointIndicator ) ;
      }
    }
    private void RegisterTo( EndPointBase? endPoint )
    {
      if ( null != endPoint ) {
        _toEndPoints.Add( endPoint.EndPointIndicator ) ;
      }
    }

    private void RegisterFrom( IEnumerable<IEndPointIndicator> endPoints )
    {
      _fromEndPoints.UnionWith( endPoints ) ;
    }
    private void RegisterTo( IEnumerable<IEndPointIndicator> endPoints )
    {
      _toEndPoints.UnionWith( endPoints ) ;
    }

    private readonly HashSet<IEndPointIndicator> _fromEndPoints = new() ;

    private readonly HashSet<IEndPointIndicator> _toEndPoints = new() ;

    private PassingEndPointInfo()
    {
    }

    public IEnumerable<IEndPointIndicator> FromEndPoints => _fromEndPoints ;
    public IEnumerable<IEndPointIndicator> ToEndPoints => _toEndPoints ;
  }
}