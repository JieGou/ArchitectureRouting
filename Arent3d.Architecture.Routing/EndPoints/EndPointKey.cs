using System ;
using System.Diagnostics ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  /// <summary>
  /// A specifier which determine the equality of an end point.
  /// </summary>
  [DebuggerDisplay("{Type}:{Param}")]
  public class EndPointKey : IEquatable<EndPointKey>
  {
    internal string Type { get ; }
    internal string Param { get ; }
    
    internal EndPointKey( string type, string param )
    {
      Type = type ;
      Param = param ;
    }

    public bool Equals( EndPointKey? other )
    {
      if ( other is null ) return false ;

      return ( Type == other.Type && Param == other.Param ) ;
    }

    public override bool Equals( object? obj )
    {
      if ( ReferenceEquals( null, obj ) ) return false ;
      if ( ReferenceEquals( this, obj ) ) return true ;
      if ( obj.GetType() != this.GetType() ) return false ;
      return Equals( (EndPointKey) obj ) ;
    }

    public override int GetHashCode()
    {
      return ( ( Type.GetHashCode() * 387 ) ^ Param.GetHashCode() ) ;
    }

    public static bool operator ==( EndPointKey? left, EndPointKey? right )
    {
      return Equals( left, right ) ;
    }

    public static bool operator !=( EndPointKey? left, EndPointKey? right )
    {
      return ! Equals( left, right ) ;
    }

    public string GetElementUniqueId()
    {
      const string connectorPointType = "Connector" ;
      const string terminatePointType = "Terminate Point" ;
      const string passPointType = "Pass Point" ;
      const string passPointBranchType = "Pass Point Branch" ;
      var elementId = string.Empty ;
      switch ( Type ) {
        case connectorPointType :
        {
          var deserializer = new DeserializerObject<SerializeField>( Param ) ;
          elementId = deserializer.GetString( SerializeField.ElementUniqueId ) ?? string.Empty ;
          break ;
        }
        case terminatePointType or passPointType or passPointBranchType :
          elementId = Param ;
          break ;
      }

      return elementId ;
    }
    
    public string GetTypeName()
    {
      return Type ;
    }

    private enum SerializeField
    {
      ElementUniqueId
    }
  }
}