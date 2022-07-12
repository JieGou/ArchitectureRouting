using System ;
using System.Collections.Generic ;
using System.Reflection ;
using System.Text ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  public class FieldFactory : IFieldFactory
  {
    public FieldBuilder CreateField( SchemaBuilder schemaBuilder, PropertyInfo propertyInfo )
    {
      IFieldFactory? fieldFactory = null ;

      var fieldType = propertyInfo.PropertyType ;
      if ( fieldType.IsGenericType ) {
        foreach ( var interfaceType in fieldType.GetInterfaces() ) {
          if(!interfaceType.IsGenericType)
            continue;
          
          if ( interfaceType.GetGenericTypeDefinition() == typeof( IList<> ) ) {
            fieldFactory = new ArrayFieldCreator() ;
            break ;
          }

          if ( interfaceType.GetGenericTypeDefinition() != typeof( IDictionary<,> ) )
            continue ;
          
          fieldFactory = new MapFieldCreator() ;
          break ;
        }
      }
      else {
        fieldFactory = new SimpleFieldCreator() ;
      }


      if ( fieldFactory != null )
        return fieldFactory.CreateField( schemaBuilder, propertyInfo ) ;
      
      var sb = new StringBuilder() ;
      sb.AppendLine( $"Type {fieldType} does not supported." ) ;
      sb.AppendLine( "Only IList<T> and IDictionary<TKey, TValue> generic types are supported." ) ;
      throw new NotSupportedException( sb.ToString() ) ;
    }
  }
}