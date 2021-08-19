using System.Text.RegularExpressions ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  public class ConnectorEndPoint : IEndPoint
  {
    public const string Type = "Connector" ;

    public static EndPointKey GenerateKey( Connector connector ) => GenerateKey( connector.Owner.Id, connector.Id ) ;
    private static EndPointKey GenerateKey( ElementId equipmentId, int connectorIndex )
    {
      return new EndPointKey( Type, BuildParameterString( equipmentId, connectorIndex ) ) ;
    }
    public static string BuildParameterString( Connector connector ) => BuildParameterString( connector.Owner.Id, connector.Id ) ;

    private enum SerializeField
    {
      ElementId,
      ConnectorIndex,
    }

    private static string BuildParameterString( ElementId equipmentId, int connectorIndex )
    {
      var stringifier = new SerializerObject<SerializeField>() ;

      stringifier.Add( SerializeField.ElementId, equipmentId ) ;
      stringifier.Add( SerializeField.ConnectorIndex, connectorIndex ) ;

      return stringifier.ToString() ;
    }

    public static ConnectorEndPoint? ParseParameterString( Document document, string str )
    {
      var deserializer = new DeserializerObject<SerializeField>( str ) ;

      if ( deserializer.GetElementId( SerializeField.ElementId ) is not { } elementId ) return null ;
      if ( deserializer.GetInt( SerializeField.ConnectorIndex ) is not { } connectorIndex ) return null ;

      return new ConnectorEndPoint( document, elementId, connectorIndex ) ;
    }


    public string TypeName => Type ;

    public EndPointKey Key => GenerateKey( EquipmentId, ConnectorIndex ) ;

    public bool IsReplaceable => true ;

    public bool IsOneSided => true ;

    private readonly Document _document ;

    public ElementId EquipmentId { get ; }
    public int ConnectorIndex { get ; }

    public Connector? GetConnector() => _document.GetElementById<Instance>( EquipmentId )?.GetConnectorManager()?.Lookup( ConnectorIndex ) ;

    public string ParameterString => BuildParameterString( EquipmentId, ConnectorIndex ) ;

    public XYZ RoutingStartPosition => GetConnector()?.Origin ?? XYZ.Zero ;

    public ConnectorEndPoint( Connector connector )
    {
      _document = connector.Owner.Document ;
      EquipmentId = connector.Owner.Id ;
      ConnectorIndex = connector.Id ;
    }

    public ConnectorEndPoint( Document document, ElementId equipmentId, int connectorIndex )
    {
      _document = document ;
      EquipmentId = equipmentId ;
      ConnectorIndex = connectorIndex ;
    }

    public XYZ GetRoutingDirection( bool isFrom )
    {
      return GetConnector()?.CoordinateSystem.BasisZ.ForEndPointType( isFrom ) ?? XYZ.BasisX ;
    }

    public bool HasValidElement( bool isFrom ) => ( null != GetConnector() ) ;

    public Connector? GetReferenceConnector() => GetConnector() ;

    public double? GetDiameter() => GetConnector()?.GetDiameter() ;

    public double GetMinimumStraightLength( double edgeDiameter, bool isFrom ) => 0 ;

    public (Route? Route, SubRoute? SubRoute) ParentBranch() => ( null, null ) ;

    public bool GenerateInstance( string routeName ) => false ;
    public bool EraseInstance() => false ;

    public override string ToString() => this.Stringify() ;

    public void Accept( IEndPointVisitor visitor ) => visitor.Visit( this ) ;
    public T Accept<T>( IEndPointVisitor<T> visitor ) => visitor.Visit( this ) ;
  }
}