using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  public class TerminatePointEndPoint : IEndPoint
  {
    public const string Type = "Terminate Point" ;

    private enum SerializeField
    {
      TerminatePointId,
      Diameter,
      Position,
      Direction,
      LinkedInstanceId,
    }

    public static TerminatePointEndPoint? ParseParameterString( Document document, string str )
    {
      var deserializer = new DeserializerObject<SerializeField>( str ) ;

      if ( deserializer.GetElementId( SerializeField.TerminatePointId ) is not { } terminatePointId ) return null ;
      var diameter = deserializer.GetDouble( SerializeField.Diameter ) ;
      if ( deserializer.GetXYZ( SerializeField.Position ) is not { } position ) return null ;
      if ( deserializer.GetXYZ( SerializeField.Direction ) is not { } direction ) return null ;
      if ( deserializer.GetElementId( SerializeField.LinkedInstanceId ) is not { } linkedInstanceId ) return null ;

      return new TerminatePointEndPoint( document, terminatePointId, position, direction, diameter * 0.5, linkedInstanceId ) ;
    }

    public string ParameterString
    {
      get
      {
        var stringifier = new SerializerObject<SerializeField>() ;

        stringifier.Add( SerializeField.TerminatePointId, TerminatePointId ) ;
        stringifier.Add( SerializeField.Diameter, GetDiameter() ) ;
        stringifier.AddNonNull( SerializeField.Position, RoutingStartPosition ) ;
        stringifier.AddNonNull( SerializeField.Direction, Direction ) ;
        stringifier.Add( SerializeField.LinkedInstanceId, LinkedInstanceId ) ;

        return stringifier.ToString() ;
      }
    }


    public string TypeName => Type ;
    public EndPointKey Key => new EndPointKey( TypeName, TerminatePointId.IntegerValue.ToString() ) ;

    public bool IsReplaceable => true ;

    public bool IsOneSided => true ;

    private readonly Document _document ;

    public ElementId TerminatePointId { get ; private set ; }
    public ElementId LinkedInstanceId { get ; private set ; }

    public Instance? GetTerminatePoint() => _document.GetElementById<Instance>( TerminatePointId ) ;

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

    public double GetMinimumStraightLength( double edgeDiameter, bool isFrom ) => 0 ;

    public (Route? Route, SubRoute? SubRoute) ParentBranch() => ( null, null ) ;

    public bool GenerateInstance( string routeName )
    {
      if ( null != GetTerminatePoint() ) return false ;

      TerminatePointId = _document.AddTerminatePoint( routeName, PreferredPosition, PreferredDirection, PreferredRadius ).Id ;

        Element elemTerP = _document.GetElement( TerminatePointId );
        Element elemOrg = _document.GetElement( LinkedInstanceId );

        foreach ( Parameter parameter in elemTerP.Parameters ) {
            if ( parameter.Definition.Name == "LinkedInstanceId" ) {
                parameter.Set( LinkedInstanceId.ToString() );
            }
            if ( parameter.Definition.Name == "LinkedInstanceXYZ" ) {
                LocationPoint? Lp = elemOrg.Location as LocationPoint;
                XYZ? ElementPoint = Lp?.Point;
                XYZ addPoint = PreferredPosition - ElementPoint;

                parameter.Set( addPoint.ToString().Substring(1,addPoint.ToString().Length -1 ) );
            }
        }

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