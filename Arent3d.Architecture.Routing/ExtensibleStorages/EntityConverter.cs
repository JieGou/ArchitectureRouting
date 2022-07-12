using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Extensions ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  public class EntityConverter : IEntityConverter
  {
    private readonly ISchemaCreator _schemaCreator ;
    private readonly AttributeExtractor<FieldAttribute> _fieldAttributeExtractor = new() ;

    public EntityConverter( ISchemaCreator schemaCreator )
    {
      _schemaCreator = schemaCreator ;
    }

    public Entity Convert( IModelEntity modelEntity )
    {
      var modelType = modelEntity.GetType() ;
      var schema = _schemaCreator.FindOrCreate( modelType ) ;
      var entity = new Entity( schema ) ;

      var schemaFields = schema.ListFields() ;
      foreach ( var field in schemaFields ) {
        var propertyInfo = modelType.GetProperty( field.FieldName ) ;
        if ( null == propertyInfo )
          continue ;

        var fieldAttribute = _fieldAttributeExtractor.GetAttribute( propertyInfo ) ;
        dynamic propertyValue = propertyInfo.GetValue( modelEntity ) ;
        if ( propertyValue is null )
          continue ;

        switch ( field.ContainerType ) {
          case ContainerType.Simple :
            propertyValue = ConvertSimpleProperty( propertyValue, field ) ;

            if ( field.GetSpecTypeId().Empty() ) {
              entity.Set( field, propertyValue ) ;
            }
            else if ( IsCompatibleUnitType( field, fieldAttribute.UnitTypeId ) ) {
              entity.Set( field, propertyValue, new ForgeTypeId( fieldAttribute.UnitTypeId ) ) ;
            }

            break ;
          case ContainerType.Array :
            if ( propertyValue.Count == 0 )
              continue ;

            var convertedArrayFieldValue = ConvertArrayProperty( propertyValue, field ) ;
            if ( field.GetSpecTypeId().Empty() ) {
              EntityExtension.SetWrapper( entity, field, convertedArrayFieldValue ) ;
            }
            else if ( IsCompatibleUnitType( field, fieldAttribute.UnitTypeId ) ) {
              EntityExtension.SetWrapper( entity, field, convertedArrayFieldValue, new ForgeTypeId( fieldAttribute.UnitTypeId ) ) ;
            }

            break ;

          case ContainerType.Map :
            if ( propertyValue.Count == 0 )
              continue ;

            var convertedMapFieldValue = ConvertMapProperty( propertyValue, field ) ;
            if ( field.GetSpecTypeId().Empty() ) {
              EntityExtension.SetWrapper( entity, field, convertedMapFieldValue ) ;
            }
            else if ( IsCompatibleUnitType( field, fieldAttribute.UnitTypeId ) ) {
              EntityExtension.SetWrapper( entity, field, convertedMapFieldValue, new ForgeTypeId( fieldAttribute.UnitTypeId ) ) ;
            }

            break ;
          default :
            throw new NotSupportedException( $"Unknown {typeof( ContainerType ).FullName}." ) ;
        }
      }

      return entity ;
    }

    public TModelEntity Convert<TModelEntity>( Entity entity ) where TModelEntity : class, IModelEntity
    {
      var modelType = typeof( TModelEntity ) ;
      var modelInstance = Activator.CreateInstance<TModelEntity>() ;

      var schemaFields = entity.Schema.ListFields() ;
      foreach ( var field in schemaFields ) {
        var propertyInfo = modelType.GetProperty( field.FieldName ) ;
        if ( null == propertyInfo )
          continue ;

        var fieldAttribute = _fieldAttributeExtractor.GetAttribute( propertyInfo ) ;
        object? entityValue = null ;
        switch ( field.ContainerType ) {
          case ContainerType.Simple :
            entityValue = GetEntityFieldValue( entity, field, field.ValueType, fieldAttribute.UnitTypeId ) ;
            if ( entityValue is Entity subEntity && subEntity.Schema != null )
              entityValue = Convert( propertyInfo.PropertyType, subEntity ) ;

            break ;
          case ContainerType.Array :
            var listType = typeof( IList<> ) ;
            var genericListType = listType.MakeGenericType( field.ValueType ) ;
            entityValue = GetEntityFieldValue( entity, field, genericListType, fieldAttribute.UnitTypeId ) ;

            if ( entityValue is not IList listEntityValues )
              continue ;

            IList listProperty ;
            if ( propertyInfo.PropertyType.GetConstructor( new[] { typeof( int ) } ) != null ) {
              listProperty = (IList) Activator.CreateInstance( propertyInfo.PropertyType, listEntityValues.Count ) ;
            }
            else {
              listProperty = (IList) Activator.CreateInstance( propertyInfo.PropertyType ) ;
            }

            if ( field.ValueType == typeof( Entity ) ) {
              var subModelType = propertyInfo.PropertyType.GetGenericArguments()[ 0 ] ;
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
            entityValue = GetEntityFieldValue( entity, field, genericDicitionaryType, fieldAttribute.UnitTypeId ) ;

            if ( entityValue is not IDictionary mapEntityValues )
              continue ;

            IDictionary dictProperty ;
            if ( propertyInfo.PropertyType.GetConstructor( new[] { typeof( int ) } ) != null ) {
              dictProperty = (IDictionary) Activator.CreateInstance( propertyInfo.PropertyType, mapEntityValues.Count ) ;
            }
            else {
              dictProperty = (IDictionary) Activator.CreateInstance( propertyInfo.PropertyType ) ;
            }

            if ( field.ValueType == typeof( Entity ) ) {
              var subModelType = propertyInfo.PropertyType.GetGenericArguments()[ 1 ] ;

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
          propertyInfo.SetValue( modelInstance, entityValue ) ;
      }

      return modelInstance ;
    }

    #region Methods

    private object ConvertMapProperty( dynamic propertyValue, Field field )
    {
      if ( field.ContainerType != ContainerType.Map )
        throw new InvalidOperationException( "Field is not a IDictionary type." ) ;
      
      Type propertyValueType = propertyValue.GetType() ;
      var isImplementDictionaryInterface = propertyValueType.GetInterfaces().Any( x => x.GetGenericTypeDefinition() == typeof( IDictionary<,> ) ) ;
      if ( ! isImplementDictionaryInterface )
        throw new NotSupportedException( $"Unsupported type {propertyValueType.Name}." ) ;

      if ( field.ValueType != typeof( Entity ) )
        return propertyValue ;

      var dictionaryType = typeof( Dictionary<,> ).MakeGenericType( field.KeyType, typeof( Entity ) ) ;

      var mapArray = (IDictionary) Activator.CreateInstance( dictionaryType, new object[] { propertyValue.Count } ) ;
      foreach ( var keyValuePair in propertyValue ) {
        var convertedEntity = Convert( keyValuePair.Value ) ;
        mapArray.Add( keyValuePair.Key, convertedEntity ) ;
      }

      return mapArray ;
    }

    private object ConvertSimpleProperty( dynamic propertyValue, Field field )
    {
      if ( field.ContainerType != ContainerType.Simple )
        throw new InvalidOperationException( "Field is not a simple type." ) ;

      if ( field.ValueType == typeof( Entity ) )
        propertyValue = Convert( propertyValue ) ;

      return propertyValue ;
    }

    private object ConvertArrayProperty( dynamic propertyValue, Field field )
    {
      if ( field.ContainerType != ContainerType.Array )
        throw new InvalidOperationException( "Field is not a IList type." ) ;
      
      Type propertyValueType = propertyValue.GetType() ;
      var isImplementListInterface = propertyValueType.GetInterfaces().Any( x => x.GetGenericTypeDefinition() == typeof( IList<> ) ) ;
      if ( ! isImplementListInterface )
        throw new NotSupportedException( $"Unsupported type {propertyValueType.Name}." ) ;

      if ( field.ValueType != typeof( Entity ) )
        return propertyValue ;

      IList<Entity> entityList = new List<Entity>( propertyValue.Count ) ;
      foreach ( IModelEntity modelEntity in propertyValue ) {
        var convertedEntity = Convert( modelEntity ) ;
        entityList.Add( convertedEntity ) ;
      }

      return entityList ;
    }

    private object Convert( Type modelEntityType, Entity entity )
    {
      var convertMethod = GetType().GetMethod( nameof( Convert ), new[] { typeof( Entity ) } ) ;
      if ( null == convertMethod )
        throw new InvalidOperationException( $"Not found the {nameof( Convert )} method of the {modelEntityType.Name} type." ) ;

      var convertMethodGeneric = convertMethod.MakeGenericMethod( modelEntityType ) ;
      var modelEntity = convertMethodGeneric.Invoke( this, new object[] { entity } ) ;
      return modelEntity ;
    }

    private object? GetEntityFieldValue( Entity entity, Field field, Type fieldValueType, string unitType )
    {
      if ( field.SubSchemaGUID != Guid.Empty && field.SubSchema == null )
        return null ;

      object? entityValue = null ;
      if ( field.GetSpecTypeId().Empty() ) {
        var entityGetMethod = entity.GetType().GetMethod( nameof( Entity.Get ), new[] { typeof( Field ) } ) ;
        if ( null == entityGetMethod )
          throw new InvalidOperationException( $"Not found the {nameof( Entity.Get )} method of the {typeof(Entity)} type." ) ;

        var entityGetMethodGeneric = entityGetMethod.MakeGenericMethod( fieldValueType ) ;
        entityValue = entityGetMethodGeneric.Invoke( entity, new object[] { field } ) ;
      }
      else {
        if ( ! IsCompatibleUnitType( field, unitType ) )
          return entityValue ;

        var entityGetMethod = entity.GetType().GetMethod( nameof( Entity.Get ), new[] { typeof( Field ), typeof( ForgeTypeId ) } ) ;
        if ( null == entityGetMethod )
          throw new InvalidOperationException( $"Not found the {nameof( Entity.Get )} method of the {typeof(Entity)} type." ) ;

        var entityGetMethodGeneric = entityGetMethod.MakeGenericMethod( fieldValueType ) ;
        entityValue = entityGetMethodGeneric.Invoke( entity, new object[] { field, new ForgeTypeId( unitType ) } ) ;
      }

      return entityValue ;
    }

    private static bool IsCompatibleUnitType( Field field, string unitTypeId )
    {
      if ( ! field.CompatibleUnit( new ForgeTypeId( unitTypeId ) ) )
        throw new ArgumentException( $"At field {field.FieldName}, {nameof( UnitTypeId )} don't compatible with {nameof( SpecTypeId )}." ) ;

      return true ;
    }

    #endregion
  }
}