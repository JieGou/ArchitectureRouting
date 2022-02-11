using System.Diagnostics ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  [DebuggerDisplay("{Key}")]
  public class TerminatePointEndPoint : IRealEndPoint
  {
    public const string Type = "Terminate Point" ;

    private enum SerializeField
    {
      TerminatePointUniqueId,
      Diameter,
      Position,
      Direction,
      LinkedInstanceUniqueId,
    }

    public static TerminatePointEndPoint? ParseParameterString( Document document, string str )
    {
      var deserializer = new DeserializerObject<SerializeField>( str ) ;

      if ( deserializer.GetString( SerializeField.TerminatePointUniqueId ) is not { } terminatePointUniqueId ) return null ;
      var diameter = deserializer.GetDouble( SerializeField.Diameter ) ;
      if ( deserializer.GetXYZ( SerializeField.Position ) is not { } position ) return null ;
      if ( deserializer.GetXYZ( SerializeField.Direction ) is not { } direction ) return null ;
      if ( deserializer.GetString( SerializeField.LinkedInstanceUniqueId ) is not { } linkedInstanceUniqueId ) return null ;

      return new TerminatePointEndPoint( document, terminatePointUniqueId, position, direction, diameter * 0.5, linkedInstanceUniqueId ) ;
    }

    public string ParameterString
    {
      get
      {
        var stringifier = new SerializerObject<SerializeField>() ;

        stringifier.AddNonNull( SerializeField.TerminatePointUniqueId, TerminatePointUniqueId ) ;
        stringifier.Add( SerializeField.Diameter, GetDiameter() ) ;
        stringifier.AddNonNull( SerializeField.Position, RoutingStartPosition ) ;
        stringifier.AddNonNull( SerializeField.Direction, Direction ) ;
        stringifier.AddNonNull( SerializeField.LinkedInstanceUniqueId, LinkedInstanceUniqueId ) ;

        return stringifier.ToString() ;
      }
    }


    public string TypeName => Type ;
    public string DisplayTypeName => "EndPoint.DisplayTypeName.Terminal".GetAppStringByKeyOrDefault( TypeName ) ;
    public EndPointKey Key => new EndPointKey( TypeName, TerminatePointUniqueId ) ;

    internal static TerminatePointEndPoint? FromKeyParam( Document document, string param )
    {
      if ( document.GetElementById<FamilyInstance>( param ) is not { } instance ) return null ;
      if ( instance.Symbol.Id != document.GetFamilySymbols( RoutingFamilyType.TerminatePoint ).FirstOrDefault()?.Id ) return null ;

      return new TerminatePointEndPoint( instance, null ) ;
    }

    public bool IsReplaceable => true ;

    public bool IsOneSided => true ;

    private readonly Document _document ;

    public string TerminatePointUniqueId { get ; private set ; }
    public string LinkedInstanceUniqueId { get ; private set ; }

    public Instance? GetTerminatePoint() => _document.GetElementById<Instance>( TerminatePointUniqueId ) ;

    private XYZ PreferredPosition { get ; set ; } = XYZ.Zero ;

    public XYZ RoutingStartPosition => GetTerminatePoint()?.GetTotalTransform().Origin ?? PreferredPosition ;
    private XYZ PreferredDirection { get ; set ; } = XYZ.Zero ;
    public XYZ Direction => GetTerminatePoint()?.GetTotalTransform().BasisX ?? PreferredDirection ;
    private double? PreferredRadius { get ; set ; } = 0 ;

    public ElementId GetLevelId( Document document ) => GetTerminatePoint()?.GetLevelId() ?? GetElementLevelId( document, LinkedInstanceUniqueId ) ?? document.GuessLevelId( PreferredPosition ) ;

    private static ElementId? GetElementLevelId( Document document, string linkedInstanceUniqueId )
    {
      return document.GetElementById<Element>( linkedInstanceUniqueId )?.GetLevelId() ;
    }

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
      PreferredRadius = terminatePoint.LookupParameter( "Arent-RoundDuct-Diameter" )?.AsDouble() * 0.5 ;
    }

    public TerminatePointEndPoint( Instance instance, Instance? linkedInstance )
    {
      _document = instance.Document ;
      TerminatePointUniqueId = instance.UniqueId ;
      LinkedInstanceUniqueId = linkedInstance?.UniqueId ?? string.Empty ;

      SetPreferredParameters( instance ) ;
    }

    public TerminatePointEndPoint( Document document, string terminatePointUniqueId, XYZ preferredPosition, XYZ preferredDirection, double? preferredRadius, string linkedInstanceUniqueId )
    {
      _document = document ;
      TerminatePointUniqueId = terminatePointUniqueId ;
      LinkedInstanceUniqueId = linkedInstanceUniqueId ;

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

    Route? IEndPoint.ParentRoute() => null ;
    SubRoute? IEndPoint.ParentSubRoute() => null ;

    public bool GenerateInstance( string routeName )
    {
      if ( null != GetTerminatePoint() ) return false ;

      TerminatePointUniqueId = _document.AddTerminatePoint( routeName, PreferredPosition, PreferredDirection, PreferredRadius, GetLevelId( _document ) ).UniqueId ;

      Element elemTerP = _document.GetElement( TerminatePointUniqueId ) ;
      Element elemOrg = _document.GetElement( LinkedInstanceUniqueId ) ;

      foreach ( Parameter parameter in elemTerP.Parameters ) {
        if ( parameter.Definition.Name == "LinkedInstanceId" ) {
          parameter.Set( LinkedInstanceUniqueId ) ;
        }

        if ( parameter.Definition.Name == "LinkedInstanceXYZ" ) {
          LocationPoint? Lp = elemOrg.Location as LocationPoint ;
          XYZ? ElementPoint = Lp?.Point ;
          XYZ addPoint = PreferredPosition - ElementPoint ;

          parameter.Set( addPoint.ToString().Substring( 1, addPoint.ToString().Length - 1 ) ) ;
        }
      }

      elemTerP.SetProperty( PassPointParameter.RelatedConnectorUniqueId, LinkedInstanceUniqueId ) ;
      elemTerP.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, LinkedInstanceUniqueId ) ;

      return true ;
    }

    public bool EraseInstance()
    {
      UpdatePreferredParameters() ;
      return ( 0 < _document.Delete( TerminatePointUniqueId ).Count ) ;
    }

    public override string ToString() => this.Stringify() ;

    public void Accept( IEndPointVisitor visitor ) => visitor.Visit( this ) ;
    public T Accept<T>( IEndPointVisitor<T> visitor ) => visitor.Visit( this ) ;
  }
}