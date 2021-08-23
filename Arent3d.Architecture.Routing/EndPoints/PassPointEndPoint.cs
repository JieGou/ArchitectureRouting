using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  public class PassPointEndPoint : IEndPoint
  {
    public const string Type = "Pass Point" ;

    private enum SerializeField
    {
      PassPointId,
      Diameter,
      Position,
      Direction,
    }

    public static PassPointEndPoint? ParseParameterString( Document document, string str )
    {
      var deserializer = new DeserializerObject<SerializeField>( str ) ;

      if ( deserializer.GetElementId( SerializeField.PassPointId ) is not { } passPointId ) return null ;
      var diameter = deserializer.GetDouble( SerializeField.Diameter ) ;
      if ( deserializer.GetXYZ( SerializeField.Position ) is not { } position ) return null ;
      if ( deserializer.GetXYZ( SerializeField.Direction ) is not { } direction ) return null ;

      return new PassPointEndPoint( document, passPointId, position, direction, diameter * 0.5 ) ;
    }

    public string ParameterString
    {
      get
      {
        var stringifier = new SerializerObject<SerializeField>() ;

        stringifier.Add( SerializeField.PassPointId, PassPointId ) ;
        stringifier.Add( SerializeField.Diameter, GetDiameter() ) ;
        stringifier.AddNonNull( SerializeField.Position, RoutingStartPosition ) ;
        stringifier.AddNonNull( SerializeField.Direction, Direction ) ;

        return stringifier.ToString() ;
      }
    }


    public string TypeName => Type ;
    public EndPointKey Key => new EndPointKey( TypeName, PassPointId.IntegerValue.ToString() ) ;

    public bool IsReplaceable => false ;

    public bool IsOneSided => false ;

    internal Document Document { get ; }

    public ElementId PassPointId { get ; private set ; }

    public Instance? GetPassPoint() => Document.GetElementById<Instance>( PassPointId ) ;

    private XYZ PreferredPosition { get ; set ; } = XYZ.Zero ;

    public XYZ GetIndicatorPosition( Route ownerRoute ) => RoutingStartPosition ;
    
    public XYZ RoutingStartPosition => GetPreferredStartPosition() ;
    private XYZ PreferredDirection { get ; set ; } = XYZ.Zero ;
    public XYZ Direction => GetPassPoint()?.GetTotalTransform().BasisX ?? PreferredDirection ;
    private double? PreferredRadius { get ; set ; } = 0 ;

    public void UpdatePreferredParameters()
    {
      if ( GetPassPoint() is not { } passPoint ) return ;

      SetPreferredParameters( passPoint ) ;
    }

    private void SetPreferredParameters( Instance passPoint )
    {
      var transform = passPoint.GetTotalTransform() ;
      PreferredPosition = transform.Origin ;
      PreferredDirection = transform.BasisX ;
      PreferredRadius = passPoint.LookupParameter( "Arent-RoundDuct-Diameter" )?.AsDouble() ;
    }

    /// <summary>
    /// return startposition after comparing to FixedBopHeight
    /// </summary>
    /// <returns></returns>
    private XYZ GetPreferredStartPosition()
    {
      var passPoint = GetPassPoint() ;
      var originalStartPosition = passPoint?.GetTotalTransform().Origin ?? PreferredPosition ;
      var startPosition = originalStartPosition ;

      if ( passPoint is { } passP && passP.GetRouteName() is { } routeName ) {
        var route = RouteCache.Get( passP.Document )[ routeName ] ;
        var passpointKey = this.Key ;
        var segments = GetRelatedSegments( route, passpointKey ) ;
        var targetSegment = segments?.Where( s => s.FixedBopHeight != null ).FirstOrDefault( h => ! Equals( h.FixedBopHeight, passPoint.GetTotalTransform().Origin.Z ) ) ;
        var targetHeight = targetSegment?.FixedBopHeight ;
        var targetDiameter = targetSegment?.PreferredNominalDiameter ;

        if ( targetHeight is { } fixedBopHeight && targetDiameter is {} diameter &&  passP.GetTotalTransform().Origin.Z is { } originZ ) {
          var fixedCenterHeight = fixedBopHeight + diameter/2 ;
          var difference = Math.Abs( fixedCenterHeight - originZ ) ;
          if ( difference < targetDiameter ) {
            startPosition = new XYZ( originalStartPosition.X, originalStartPosition.Y, fixedCenterHeight ) ;
          }
        }
      }

      return startPosition ;
    }

    private IEnumerable<RouteSegment>? GetRelatedSegments( Route? route, EndPointKey? pointKey )
    {
      return route?.RouteSegments.Where( s => s.FromEndPoint.Key == pointKey || s.ToEndPoint.Key == pointKey ) ;
    }

    public PassPointEndPoint( Instance instance )
    {
      Document = instance.Document ;
      PassPointId = instance.Id ;

      SetPreferredParameters( instance ) ;
    }

    public PassPointEndPoint( Document document, ElementId passPointId, XYZ preferredPosition, XYZ preferredDirection, double? preferredRadius )
    {
      Document = document ;
      PassPointId = passPointId ;

      PreferredPosition = preferredPosition ;
      PreferredDirection = ( preferredDirection.IsZeroLength() ? XYZ.BasisX : preferredDirection.Normalize() ) ;
      PreferredRadius = preferredRadius ;
      UpdatePreferredParameters() ;
    }

    public XYZ GetRoutingDirection( bool isFrom ) => Direction ;

    public bool HasValidElement( bool isFrom ) => ( null != GetPassPoint() ) ;

    public Connector? GetReferenceConnector() => null ;

    public double? GetDiameter() => GetPassPoint()?.LookupParameter( "Arent-RoundDuct-Diameter" )?.AsDouble() ?? PreferredRadius * 2 ;

    public double GetMinimumStraightLength( double edgeDiameter, bool isFrom ) => 0 ;

    public (Route? Route, SubRoute? SubRoute) ParentBranch() => ( null, null ) ;

    public bool GenerateInstance( string routeName )
    {
      if ( null != GetPassPoint() ) return true ;

      PassPointId = Document.AddPassPoint( routeName, PreferredPosition, PreferredDirection, PreferredRadius ).Id ;
      return false ;
    }

    public bool EraseInstance()
    {
      UpdatePreferredParameters() ;
      return ( 0 < Document.Delete( PassPointId ).Count ) ;
    }

    public override string ToString() => this.Stringify() ;

    public void Accept( IEndPointVisitor visitor ) => visitor.Visit( this ) ;
    public T Accept<T>( IEndPointVisitor<T> visitor ) => visitor.Visit( this ) ;
  }
}