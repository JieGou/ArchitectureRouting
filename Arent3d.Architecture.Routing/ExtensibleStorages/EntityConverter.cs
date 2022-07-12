using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  public class EntityConverter : IEntityConverter
  {
    private readonly ISchemaCreator _schemaCreator;
    
    public EntityConverter(ISchemaCreator schemaCreator)
    {
      _schemaCreator = schemaCreator;
    }

    public Entity Convert( IModelEntity modelEntity )
    {
      var modelType = modelEntity.GetType() ;
      var schema = _schemaCreator.CreateSchema( modelType ) ;
      var entity = new Entity( schema ) ;
      
      var schemaFields = schema.ListFields() ;
      foreach ( var field in schemaFields ) {
        var property = modelType.GetProperty( field.FieldName ) ;
        if(null == property)
          continue;
        
        dynamic propertyValue = property.GetValue( modelEntity ) ;
        if ( propertyValue is null )
          continue ;

        switch ( field.ContainerType ) {
          case ContainerType.Simple :
            propertyValue = ConvertSimpleProperty( propertyValue, field ) ;
            entity.Set( field, propertyValue ) ;

            break ;
          case ContainerType.Array :
            if ( propertyValue.Count == 0 )
              continue ;

            var convertedArrayFieldValue = ConvertArrayProperty( propertyValue, field ) ;
            EntityExtension.SetWrapper( entity, field, convertedArrayFieldValue ) ;

            break ;

          case ContainerType.Map :
            if ( propertyValue.Count == 0 )
              continue ;
            
            var convertedMapFieldValue = ConvertMapProperty( propertyValue, field ) ;
            EntityExtension.SetWrapper( entity, field, convertedMapFieldValue ) ;

            break ;
          default :
            throw new NotSupportedException( $"Unknown {typeof(ContainerType).FullName}." ) ;
        }
      }

      return entity ;
    }

    public TModelEntity Convert<TModelEntity>( Entity entity ) where TModelEntity : class, IModelEntity
    {
      var modelType = typeof( TModelEntity ) ;
      var modelInstance = Activator.CreateInstance<TModelEntity>() ;

      var schema = entity.Schema ;
      var schemaFields = schema.ListFields() ;
      foreach ( var field in schemaFields ) {
        var property = modelType.GetProperty( field.FieldName ) ;
        if(null == property)
          continue;
        
        object? entityValue = null ;
        switch ( field.ContainerType ) {
          case ContainerType.Simple :
            entityValue = GetEntityFieldValue( entity, field, field.ValueType ) ;
            if ( entityValue is Entity subEntity && subEntity.Schema != null)
              entityValue = Convert( property.PropertyType, subEntity ) ;
            
            break ;
          case ContainerType.Array :
            var listType = typeof( IList<> ) ;
            var genericListType = listType.MakeGenericType( field.ValueType ) ;
            entityValue = GetEntityFieldValue( entity, field, genericListType ) ;

            if(entityValue is not IList listEntityValues)
              continue;

            IList listProperty ;
            if ( property.PropertyType.GetConstructor( new[] { typeof( int ) } ) != null ) {
              listProperty = ( IList ) Activator.CreateInstance( property.PropertyType, listEntityValues.Count ) ;
            }
            else {
              listProperty = ( IList ) Activator.CreateInstance( property.PropertyType ) ;
            }
            
            if ( field.ValueType == typeof( Entity ) ) {
              var subModelType = property.PropertyType.GetGenericArguments()[ 0 ] ;
              foreach ( Entity listEntityValue in listEntityValues ) {
                var convertedEntity = Convert( subModelType, listEntityValue ) ;
                listProperty.Add( convertedEntity ) ;
              }
            }
            else {
              foreach ( var listEntityValue in listEntityValues ) {
                listProperty.Add( listEntityValue ) ;
              }
            }

            entityValue = listProperty ;

            break ;
          case ContainerType.Map :
            var dicitonaryType = typeof( IDictionary<,> ) ;
            var genericDicitionaryType = dicitonaryType.MakeGenericType( field.KeyType, field.ValueType ) ;
            entityValue = GetEntityFieldValue( entity, field, genericDicitionaryType ) ;
            
            if ( entityValue is not IDictionary mapEntityValues ) 
              continue ;

            IDictionary dictProperty ;
            if ( property.PropertyType.GetConstructor( new[] { typeof( int ) } ) != null ) {
              dictProperty = ( IDictionary ) Activator.CreateInstance( property.PropertyType, mapEntityValues.Count ) ;
            }
            else {
              dictProperty = ( IDictionary ) Activator.CreateInstance( property.PropertyType ) ;
            }
            
            if ( field.ValueType == typeof( Entity ) ) {
              var subModelType = property.PropertyType.GetGenericArguments()[ 1 ] ;

              foreach ( dynamic keyValuePair in mapEntityValues ) {
                var convertedEntity = Convert( subModelType, keyValuePair.Value ) ;
                dictProperty.Add( keyValuePair.Key, convertedEntity ) ;
              }
            }
            else {
              foreach ( dynamic keyValuePair in mapEntityValues ) {
                dictProperty.Add( keyValuePair.Key, keyValuePair.Value ) ;
              }
            }

            entityValue = dictProperty ;

            break ;
        }

        if ( entityValue != null )
          property.SetValue( modelInstance, entityValue ) ;
      }

      return modelInstance ;
    }

    private object ConvertMapProperty( dynamic propertyValue, Field field )
    {
      Type propertyValueType = propertyValue.GetType() ;
      var isImplementIDictionaryInterface = propertyValueType.GetInterfaces().Any( x => x.GetGenericTypeDefinition() == typeof( IDictionary<,> ) ) ;
      if ( ! isImplementIDictionaryInterface )
        throw new NotSupportedException( "Unsupported type." ) ;

      if ( field.ValueType != typeof( Entity ) ) 
        return propertyValue ;
      
      var dictionaryType = typeof( Dictionary<,> ).MakeGenericType( field.KeyType, typeof( Entity ) ) ;

      var mapArray = ( IDictionary ) Activator.CreateInstance( dictionaryType, new object[] { propertyValue.Count } ) ;
      foreach ( var keyValuePair in propertyValue ) {
        var convertedEntity = Convert( keyValuePair.Value ) ;
        mapArray.Add( keyValuePair.Key, convertedEntity ) ;
      }

      return mapArray ;
    }
    
    private object ConvertSimpleProperty(dynamic propertyValue, Field field)
    {
      if (field.ContainerType != ContainerType.Simple)
        throw new InvalidOperationException("Field is not a simple type.");
      
      if (field.ValueType == typeof (Entity))
        propertyValue = Convert(propertyValue);
      
      return propertyValue;
    }

    private object ConvertArrayProperty( dynamic propertyValue, Field field )
    {
      Type propertyValueType = propertyValue.GetType() ;
      var isImplementIListInterface = propertyValueType.GetInterfaces().Any( x => x.GetGenericTypeDefinition() == typeof( IList<> ) ) ;
      if ( ! isImplementIListInterface ) 
        throw new NotSupportedException( "Unsupported type." ) ;

      if ( field.ValueType != typeof( Entity ) ) 
        return propertyValue ;
      
      IList<Entity> entityList = new List<Entity>( propertyValue.Count ) ;
      foreach ( IModelEntity modelEntity in propertyValue ) {
        var convertedEntity = Convert( modelEntity ) ;
        entityList.Add( convertedEntity ) ;
      }

      return entityList ;
    }
    
    private object Convert(Type modelEntityType, Entity entity)
    {
      var convertMethod = GetType().GetMethod(nameof(Convert), new[] { typeof(Entity) });
      if ( null == convertMethod )
        throw new InvalidOperationException($"Not found the {nameof(Convert)} method.") ;
      
      var convertMethodGeneric = convertMethod.MakeGenericMethod(modelEntityType);
      var iEntity = convertMethodGeneric.Invoke(this, new object[] { entity });
      return iEntity;
    }
    
    private static object? GetEntityFieldValue(Entity entity, Field field, Type fieldValueType)
    {
      if (field.SubSchemaGUID != Guid.Empty && field.SubSchema == null)
        return null;

      var entityGetMethod = entity.GetType().GetMethod(nameof(Entity.Get), new[] {typeof (Field)});
      if(null == entityGetMethod)
        throw new InvalidOperationException($"Not found the {nameof(Entity.Get)} method.") ;
      
      var entityGetMethodGeneric = entityGetMethod.MakeGenericMethod(fieldValueType);
      var entityValue = entityGetMethodGeneric.Invoke(entity, new object[] {field}) ;
      return entityValue;
    }
  }
}