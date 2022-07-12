using System ;
using System.Reflection ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages.Attributes
{
  /// <summary>
  /// The helper class which helps to find value if the TAttribute of a member info
  /// </summary>
  /// <typeparam name="TAttribute">Generic Attribute</typeparam>
  public class AttributeExtractor<TAttribute> where TAttribute : Attribute
  {
    public TAttribute GetAttribute( MemberInfo memberInfo )
    {
      var attributes = memberInfo.GetCustomAttributes( typeof( TAttribute ), false ) ;
      if ( attributes.Length == 0 )
        throw new InvalidOperationException( $"MemberInfo {memberInfo} does not have a {typeof( TAttribute )}" ) ;

      if ( attributes[ 0 ] is not TAttribute atribute )
        throw new InvalidOperationException( $"MemberInfo {memberInfo} does not have a {typeof( TAttribute )}" ) ;

      return atribute ;
    }
  }
}