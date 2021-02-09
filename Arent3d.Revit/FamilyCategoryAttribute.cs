using System ;
using System.Collections.Generic ;
using System.Reflection ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  [AttributeUsage( AttributeTargets.Field )]
  public class FamilyCategoryAttribute : Attribute
  {
    public BuiltInCategory Category { get ; }

    public FamilyCategoryAttribute( BuiltInCategory builtInCategory )
    {
      Category = builtInCategory ;
    }

    public static IReadOnlyDictionary<TEnum, BuiltInCategory> ToDictionary<TEnum>() where TEnum : Enum
    {
      var dic = new Dictionary<TEnum, BuiltInCategory>() ;

      foreach ( var fieldInfo in typeof( TEnum ).GetFields() ) {
        var attr = fieldInfo.GetCustomAttribute<FamilyCategoryAttribute>() ;
        if ( null == attr ) continue ;

        dic.Add( (TEnum) fieldInfo.GetValue( null ), attr.Category ) ;
      }

      return dic ;
    }
  }
}