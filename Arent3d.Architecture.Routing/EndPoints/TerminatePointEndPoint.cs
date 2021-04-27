using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  public class TerminatePointEndPoint : IEndPoint
  {
    public const string Type = "Terminate Point" ;

    private static readonly char[] Separator = { '/' } ;

    public static TerminatePointEndPoint? ParseParameterString( Document document, string str )
    {
      var array = str.Split( Separator ) ;
      if ( array.Length < 9 ) return null ;

      if ( false == int.TryParse( array[ 0 ], out var terminatePointId ) ) return null ;
      var radius = double.TryParse( array[ 1 ], out var diameter ) ? (double?) ( diameter * 0.5 ) : null ;
      if ( false == double.TryParse( array[ 2 ], out var posX ) ) return null ;
      if ( false == double.TryParse( array[ 3 ], out var posY ) ) return null ;
      if ( false == double.TryParse( array[ 4 ], out var posZ ) ) return null ;
      if ( false == double.TryParse( array[ 5 ], out var dirX ) ) return null ;
      if ( false == double.TryParse( array[ 6 ], out var dirY ) ) return null ;
      if ( false == double.TryParse( array[ 7 ], out var dirZ ) ) return null ;
      if ( false == int.TryParse( array[ 8 ], out var linkedInstanceId ) ) return null ;

      return new TerminatePointEndPoint( document, new ElementId( terminatePointId ), new XYZ( posX, posY, posZ ), new XYZ( dirX, dirY, dirZ ), radius, new ElementId( linkedInstanceId ) ) ;
    }


    public string TypeName => Type ;
    public EndPointKey Key => new EndPointKey( TypeName, TerminatePointId.IntegerValue.ToString() ) ;

    public bool IsOneSided => true ;

    private readonly Document _document ;

    public ElementId TerminatePointId { get ; private set ; }
    public ElementId LinkedInstanceId { get ; private set ; }

    public Instance? GetTerminatePoint() => _document.GetElementById<Instance>( TerminatePointId ) ;

    public string ParameterString => $"{TerminatePointId.IntegerValue}/{GetDiameter()?.ToString() ?? "---"}/{RoutingStartPosition.X}/{RoutingStartPosition.Y}/{RoutingStartPosition.Z}/{Direction.X}/{Direction.Y}/{Direction.Z}/{LinkedInstanceId.IntegerValue}" ;

    private XYZ PreferredPosition { get ; set ; } = XYZ.Zero ;
    public XYZ RoutingStartPosition => GetTerminatePoint()?.GetTotalTransform().Origin ?? PreferredPosition ;
    private XYZ PreferredDirection { get ; set ; } = XYZ.Zero ;
    public XYZ Direction => GetTerminatePoint()?.GetTotalTransform().BasisX ?? PreferredDirection ;
    private double? PreferredRadius { get ; set ; } = 0 ;

    public void UpdatePreferredParameters()
    {
      if ( GetTerminatePoint() is not { } terminatePoint ) return ;

      SetPreferredParameters( terminatePoint ) ;
    }

    private void SetPreferredParameters( Instance terminatePoint )
    {
      var transform = terminatePoint.GetTotalTransform() ;
      PreferredPosition = transform.Origin ;
      PreferredDirection = transform.BasisX ;
      PreferredRadius = terminatePoint.LookupParameter( "Arent-RoundDuct-Diameter" )?.AsDouble() ;
    }

    public TerminatePointEndPoint( Instance instance, Instance? linkedInstance )
    {
      _document = instance.Document ;
      TerminatePointId = instance.Id ;
      LinkedInstanceId = linkedInstance.GetValidId() ;

      SetPreferredParameters( instance ) ;
    }

    public TerminatePointEndPoint( Document document, ElementId terminatePointId, XYZ preferredPosition, XYZ preferredDirection, double? preferredRadius, ElementId linkedInstanceId )
    {
      _document = document ;
      TerminatePointId = terminatePointId ;
      LinkedInstanceId = linkedInstanceId ;

      PreferredPosition = preferredPosition ;
      PreferredDirection = ( preferredDirection.IsZeroLength() ? XYZ.BasisX : preferredDirection.Normalize() ) ;
      PreferredRadius = preferredRadius ;
      UpdatePreferredParameters() ;
    }

    public XYZ GetRoutingDirection( bool isFrom ) => Direction ;

    public bool HasValidElement( bool isFrom ) => ( null != GetTerminatePoint() ) ;

    public Connector? GetReferenceConnector() => null ;

    public double? GetDiameter() => GetTerminatePoint()?.LookupParameter( "Arent-RoundDuct-Diameter" )?.AsDouble() ?? PreferredRadius * 2 ;

    public double GetMinimumStraightLength( RouteMEPSystem routeMepSystem, double edgeDiameter, bool isFrom ) => 0 ;

    public (Route? Route, SubRoute? SubRoute) ParentBranch() => ( null, null ) ;

    public bool GenerateInstance( string routeName )
    {
      if ( null != GetTerminatePoint() ) return false ;

      TerminatePointId = _document.AddTerminatePoint( routeName, PreferredPosition, PreferredDirection, PreferredRadius ).Id ;
      return true ;
    }

    public bool EraseInstance()
    {
      UpdatePreferredParameters() ;
      return ( 0 < _document.Delete( TerminatePointId ).Count ) ;
    }

    public override string ToString() => this.Stringify() ;

    public void Accept( IEndPointVisitor visitor ) => visitor.Visit( this ) ;
    public T Accept<T>( IEndPointVisitor<T> visitor ) => visitor.Visit( this ) ;
  }
}