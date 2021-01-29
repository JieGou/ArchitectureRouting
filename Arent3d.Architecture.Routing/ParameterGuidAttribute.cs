using System ;
using System.Collections.Generic ;
using System.Reflection ;

namespace Arent3d.Architecture.Routing
{
  [AttributeUsage( AttributeTargets.Field )]
  public class ParameterGuidAttribute : Attribute
  {
    public Guid Guid { get ; }

    public ParameterGuidAttribute( string guid )
    {
      Guid = new Guid( guid ) ;
    }

    public static IReadOnlyDictionary<TEnum, Guid> ToDictionary<TEnum>() where TEnum : Enum
    {
      var dic = new Dictionary<TEnum, Guid>() ;

      foreach ( var fieldInfo in typeof( TEnum ).GetFields() ) {
        var attr = fieldInfo.GetCustomAttribute<ParameterGuidAttribute>() ;
        if ( null == attr ) continue ;

        dic.Add( (TEnum) fieldInfo.GetValue( null ), attr.Guid ) ;
      }

      return dic ;
    }

    public static IReadOnlyDictionary<Guid, TEnum> ToReverseDictionary<TEnum>() where TEnum : Enum
    {
      var dic = new Dictionary<Guid, TEnum>() ;

      foreach ( var fieldInfo in typeof( TEnum ).GetFields() ) {
        var attr = fieldInfo.GetCustomAttribute<ParameterGuidAttribute>() ;
        if ( null == attr ) continue ;

        dic.Add( attr.Guid, (TEnum) fieldInfo.GetValue( null ) ) ;
      }

      return dic ;
    }
  }
}