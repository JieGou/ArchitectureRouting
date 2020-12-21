using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture
{
  static class CecilUtil
  {
    public static T? GetCustomAttribute<T>( this Mono.Cecil.ICustomAttributeProvider assembly ) where T : Attribute
    {
      var attr = assembly.CustomAttributes.FirstOrDefault( t => t.AttributeType.FullName == typeof( T ).FullName );
      if ( null == attr ) return null;

      var argValues = attr.ConstructorArguments;
      var args = new object[argValues.Count];
      for ( int i = 0 ; i < argValues.Count ; ++i ) {
        args[i] = argValues[i].Value;
      }

      var type = typeof( T );
      var result = (T)Activator.CreateInstance( type, args );

      foreach ( var field in attr.Fields ) {
        if ( type.GetField( field.Name, BindingFlags.Public ) is not { } fieldInfo ) continue;

        fieldInfo.SetValue( result, field.Argument.Value );
      }

      foreach ( var prop in attr.Properties ) {
        if ( type.GetProperty( prop.Name, BindingFlags.Public ) is not { } propInfo ) continue;
        if ( propInfo.GetSetMethod() is not { } setter ) continue;

        setter.Invoke( result, new[] { prop.Argument.Value } );
      }

      return result;
    }

    public static IEnumerable<TypeDefinition> GetTypes( this AssemblyDefinition assembly )
    {
      return assembly.Modules.SelectMany( module => module.Types );
    }
  }
}
