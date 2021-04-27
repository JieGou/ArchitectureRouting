using System ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  /// <summary>
  /// A specifier which determine the equality of an end point.
  /// </summary>
  public class EndPointKey : IEquatable<EndPointKey>
  {
    private readonly string _type ;
    private readonly string _param ;
    
    internal EndPointKey( string type, string param )
    {
      _type = type ;
      _param = param ;
    }

    public bool Equals( EndPointKey? other )
    {
      if ( other is null ) return false ;

      return ( _type == other._type && _param == other._param ) ;
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
      return ( ( _type.GetHashCode() * 387 ) ^ _param.GetHashCode() ) ;
    }

    public static bool operator ==( EndPointKey? left, EndPointKey? right )
    {
      return Equals( left, right ) ;
    }

    public static bool operator !=( EndPointKey? left, EndPointKey? right )
    {
      return ! Equals( left, right ) ;
    }
  }
}