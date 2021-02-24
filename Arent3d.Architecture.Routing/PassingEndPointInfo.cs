using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoint ;
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

    private static IEnumerable<EndPointBase> SeekEndPoints( Dictionary<IRouteEdge, PassingEndPointInfo> dic, IReadOnlyDictionary<IRouteVertex, (List<IRouteEdge> Enter, List<IRouteEdge> Exit)> linkInfo, IRouteEdge edge, bool seekFrom )
    {
      if ( false == dic.TryGetValue( edge, out var fromTo ) ) {
        fromTo = new PassingEndPointInfo() ;
        dic.Add( edge, fromTo ) ;
      }

      if ( seekFrom ) {
        if ( 0 == fromTo._fromEndPoints.Count ) {
          if ( edge.Start is TerminalPoint ) {
            fromTo.RegisterFrom( MEPSystemCreator.GetEndPoint( edge.Start.LineInfo ) ) ;
          }
          fromTo.RegisterFrom( linkInfo[ edge.Start ].Enter.SelectMany( e => SeekEndPoints( dic, linkInfo, e, true ) ) ) ;
        }

        return fromTo._fromEndPoints ;
      }
      else {
        if ( 0 == fromTo._toEndPoints.Count ) {
          if ( edge.End is TerminalPoint ) {
            fromTo.RegisterTo( MEPSystemCreator.GetEndPoint( edge.End.LineInfo ) ) ;
          }
          fromTo.RegisterTo( linkInfo[ edge.End ].Exit.SelectMany( e => SeekEndPoints( dic, linkInfo, e, false ) ) ) ;
        }

        return fromTo._toEndPoints ;
      }
    }

    private void RegisterFrom( EndPointBase? endPoint )
    {
      if ( null != endPoint ) {
        _fromEndPoints.Add( endPoint ) ;
      }
    }
    private void RegisterTo( EndPointBase? endPoint )
    {
      if ( null != endPoint ) {
        _toEndPoints.Add( endPoint ) ;
      }
    }

    private void RegisterFrom( IEnumerable<EndPointBase> endPoints )
    {
      _fromEndPoints.UnionWith( endPoints ) ;
    }
    private void RegisterTo( IEnumerable<EndPointBase> endPoints )
    {
      _toEndPoints.UnionWith( endPoints ) ;
    }

    private readonly HashSet<EndPointBase> _fromEndPoints = new HashSet<EndPointBase>() ;

    private readonly HashSet<EndPointBase> _toEndPoints = new HashSet<EndPointBase>() ;

    private PassingEndPointInfo()
    {
    }

    public IEnumerable<IEndPointIndicator> FromEndPoints => _fromEndPoints.Select( ep => ep.EndPointIndicator ) ;
    public IEnumerable<IEndPointIndicator> ToEndPoints => _toEndPoints.Select( ep => ep.EndPointIndicator ) ;
  }
}