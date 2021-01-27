using System ;
using System.Collections.Generic ;
using System.Reflection ;

namespace Arent3d.Revit
{
  [AttributeUsage( AttributeTargets.Field )]
  public class NameOnRevitAttribute : Attribute
  {
    public string Name { get ; }

    public NameOnRevitAttribute( string name )
    {
      Name = name ;
    }

    public static IReadOnlyDictionary<TEnum, string> ToDictionary<TEnum>() where TEnum : Enum
    {
      var dic = new Dictionary<TEnum, string>() ;

      foreach ( var fieldInfo in typeof( TEnum ).GetFields() ) {
        var attr = fieldInfo.GetCustomAttribute<NameOnRevitAttribute>() ;
        if ( null == attr ) continue ;
        if ( string.IsNullOrWhiteSpace( attr.Name ) ) continue ;

        dic.Add( (TEnum) fieldInfo.GetValue( null ), attr.Name ) ;
      }

      return dic ;
    }
  }
}