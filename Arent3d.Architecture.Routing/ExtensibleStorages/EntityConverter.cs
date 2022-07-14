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
        private readonly AttributeExtractor<FieldAttribute> _fieldAttributeExtractor = new AttributeExtractor<FieldAttribute>() ;

        public EntityConverter( ISchemaCreator schemaCreator )
        {
            _schemaCreator = schemaCreator ;
        }

        public Entity Convert( IDataModel dataModel )
        {
            var dataModelType = dataModel.GetType() ;
            var schema = _schemaCreator.FindOrCreate( dataModelType ) ;
            var entity = new Entity( schema ) ;

            var schemaFields = schema.ListFields() ;
            foreach ( var schemaField in schemaFields ) {
                var propertyModel = dataModelType.GetProperty( schemaField.FieldName ) ;
                if ( null == propertyModel )
                    continue ;

                var fieldAttribute = _fieldAttributeExtractor.GetAttribute( propertyModel ) ;
                dynamic propertyValue = propertyModel.GetValue( dataModel ) ;
                if ( propertyValue is null )
                    continue ;

                switch ( schemaField.ContainerType ) {
                    case ContainerType.Simple :
                        propertyValue = ConvertSimpleProperty( propertyValue, schemaField ) ;

                        if ( schemaField.GetSpecTypeId().Empty() ) {
                            entity.Set( schemaField, propertyValue ) ;
                        }
                        else if ( IsCompatibleUnitType( schemaField, fieldAttribute.UnitTypeId ) ) {
                            entity.Set( schemaField, propertyValue, new ForgeTypeId( fieldAttribute.UnitTypeId ) ) ;
                        }

                        break ;
                    case ContainerType.Array :
                        if ( propertyValue.Count == 0 )
                            continue ;

                        var convertedArrayFieldValue = ConvertArrayProperty( propertyValue, schemaField ) ;
                        if ( schemaField.GetSpecTypeId().Empty() ) {
                            EntityExtension.SetWrapper( entity, schemaField, convertedArrayFieldValue ) ;
                        }
                        else if ( IsCompatibleUnitType( schemaField, fieldAttribute.UnitTypeId ) ) {
                            EntityExtension.SetWrapper( entity, schemaField, convertedArrayFieldValue, new ForgeTypeId( fieldAttribute.UnitTypeId ) ) ;
                        }

                        break ;

                    case ContainerType.Map :
                        if ( propertyValue.Count == 0 )
                            continue ;

                        var convertedMapFieldValue = ConvertMapProperty( propertyValue, schemaField ) ;
                        if ( schemaField.GetSpecTypeId().Empty() ) {
                            EntityExtension.SetWrapper( entity, schemaField, convertedMapFieldValue ) ;
                        }
                        else if ( IsCompatibleUnitType( schemaField, fieldAttribute.UnitTypeId ) ) {
                            EntityExtension.SetWrapper( entity, schemaField, convertedMapFieldValue, new ForgeTypeId( fieldAttribute.UnitTypeId ) ) ;
                        }

                        break ;
                }
            }

            return entity ;
        }

        public TDataModel Convert<TDataModel>( Entity entity ) where TDataModel : class, IDataModel
        {
            var dataModelType = typeof( TDataModel ) ;
            var dataModelInstance = Activator.CreateInstance<TDataModel>() ;

            var schemaFields = entity.Schema.ListFields() ;
            foreach ( var schemaField in schemaFields ) {
                var propertyModel = dataModelType.GetProperty( schemaField.FieldName ) ;
                if ( null == propertyModel )
                    continue ;

                var fieldAttribute = _fieldAttributeExtractor.GetAttribute( propertyModel ) ;
                object? entityValue = null ;
                switch ( schemaField.ContainerType ) {
                    case ContainerType.Simple :
                        entityValue = GetEntityFieldValue( entity, schemaField, schemaField.ValueType, fieldAttribute.UnitTypeId ) ;
                        if ( entityValue is Entity subEntity && subEntity.Schema != null )
                            entityValue = Convert( propertyModel.PropertyType, subEntity ) ;

                        break ;
                    case ContainerType.Array :
                        var listType = typeof( IList<> ) ;
                        var genericListType = listType.MakeGenericType( schemaField.ValueType ) ;
                        entityValue = GetEntityFieldValue( entity, schemaField, genericListType, fieldAttribute.UnitTypeId ) ;

                        if ( entityValue is not IList listEntityValues )
                            continue ;

                        IList listProperty ;
                        if ( propertyModel.PropertyType.GetConstructor( new[] { typeof( int ) } ) != null ) {
                            listProperty = (IList) Activator.CreateInstance( propertyModel.PropertyType, listEntityValues.Count ) ;
                        }
                        else {
                            listProperty = (IList) Activator.CreateInstance( propertyModel.PropertyType ) ;
                        }

                        if ( schemaField.ValueType == typeof( Entity ) ) {
                            var subDataModelType = propertyModel.PropertyType.GetGenericArguments()[ 0 ] ;
                            foreach ( Entity listEntityValue in listEntityValues ) {
                                var convertedEntity = Convert( subDataModelType, listEntityValue ) ;
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
                        var genericDicitionaryType = dicitonaryType.MakeGenericType( schemaField.KeyType, schemaField.ValueType ) ;
                        entityValue = GetEntityFieldValue( entity, schemaField, genericDicitionaryType, fieldAttribute.UnitTypeId ) ;

                        if ( entityValue is not IDictionary mapEntityValues )
                            continue ;

                        IDictionary dictProperty ;
                        if ( propertyModel.PropertyType.GetConstructor( new[] { typeof( int ) } ) != null ) {
                            dictProperty = (IDictionary) Activator.CreateInstance( propertyModel.PropertyType, mapEntityValues.Count ) ;
                        }
                        else {
                            dictProperty = (IDictionary) Activator.CreateInstance( propertyModel.PropertyType ) ;
                        }

                        if ( schemaField.ValueType == typeof( Entity ) ) {
                            var subDataModelType = propertyModel.PropertyType.GetGenericArguments()[ 1 ] ;

                            foreach ( dynamic keyValuePair in mapEntityValues ) {
                                var convertedEntity = Convert( subDataModelType, keyValuePair.Value ) ;
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
                    propertyModel.SetValue( dataModelInstance, entityValue ) ;
            }

            return dataModelInstance ;
        }

        #region Methods

        private object ConvertSimpleProperty( dynamic propertyValue, Field field )
        {
            if ( field.ContainerType != ContainerType.Simple )
                throw new InvalidOperationException( $"Field {field.FieldName} is not a simple type." ) ;

            if ( field.ValueType == typeof( Entity ) )
                propertyValue = Convert( propertyValue ) ;

            return propertyValue ;
        }

        private object ConvertArrayProperty( dynamic propertyValue, Field field )
        {
            if ( field.ContainerType != ContainerType.Array )
                throw new InvalidOperationException( $"Field {field.FieldName} is not a IList type." ) ;

            Type propertyValueType = propertyValue.GetType() ;
            var isImplementIListInterface = propertyValueType.GetInterfaces().Any( x => x.GetGenericTypeDefinition() == typeof( IList<> ) ) ;
            if ( ! isImplementIListInterface )
                throw new NotSupportedException( $"Unsupported type {propertyValueType.Name}." ) ;

            if ( field.ValueType != typeof( Entity ) )
                return propertyValue ;

            IList<Entity> entityList = new List<Entity>( propertyValue.Count ) ;
            foreach ( IDataModel dataModel in propertyValue ) {
                var convertedEntity = Convert( dataModel ) ;
                entityList.Add( convertedEntity ) ;
            }

            return entityList ;
        }

        private object ConvertMapProperty( dynamic propertyValue, Field field )
        {
            if ( field.ContainerType != ContainerType.Map )
                throw new InvalidOperationException( $"Field {field.FieldName} is not a IDictionary type." ) ;

            Type propertyValueType = propertyValue.GetType() ;
            var isImplementIDictionaryInterface = propertyValueType.GetInterfaces().Any( x => x.GetGenericTypeDefinition() == typeof( IDictionary<,> ) ) ;
            if ( ! isImplementIDictionaryInterface )
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

        private object Convert( Type dataModelType, Entity entity )
        {
            var convertMethod = GetType().GetMethod( nameof( Convert ), new[] { typeof( Entity ) } ) ;
            if ( null == convertMethod )
                throw new InvalidOperationException( $"Not found the {nameof( Convert )} method of the {GetType().Name} type." ) ;

            var convertMethodGeneric = convertMethod.MakeGenericMethod( dataModelType ) ;
            var modelData = convertMethodGeneric.Invoke( this, new object[] { entity } ) ;
            return modelData ;
        }

        private static object? GetEntityFieldValue( Entity entity, Field field, Type fieldValueType, string unitType )
        {
            /*
             * When you save an entity to an element and entity has a SubEntity you omit set SubEntity.
             * And there is a case that would happen when there is no SubSchema loaded into the memory.
             * In this case, Revit throws an exception about "There is no Schema with id in memory"
             */
            if ( field.SubSchemaGUID != Guid.Empty && field.SubSchema == null )
                return null ;

            object? entityValue = null ;
            if ( field.GetSpecTypeId().Empty() ) {
                var getMethod = entity.GetType().GetMethod( nameof( Entity.Get ), new[] { typeof( Field ) } ) ;
                if ( null == getMethod )
                    throw new InvalidOperationException( $"Not found the {nameof( Entity.Get )} method of the {typeof( Entity )} type." ) ;

                var getMethodGeneric = getMethod.MakeGenericMethod( fieldValueType ) ;
                entityValue = getMethodGeneric.Invoke( entity, new object[] { field } ) ;
            }
            else {
                if ( ! IsCompatibleUnitType( field, unitType ) )
                    return entityValue ;

                var getMethod = entity.GetType().GetMethod( nameof( Entity.Get ), new[] { typeof( Field ), typeof( ForgeTypeId ) } ) ;
                if ( null == getMethod )
                    throw new InvalidOperationException( $"Not found the {nameof( Entity.Get )} method of the {typeof( Entity )} type." ) ;

                var getMethodGeneric = getMethod.MakeGenericMethod( fieldValueType ) ;
                entityValue = getMethodGeneric.Invoke( entity, new object[] { field, new ForgeTypeId( unitType ) } ) ;
            }

            return entityValue ;
        }

        private static bool IsCompatibleUnitType( Field field, string unitTypeId )
        {
            if ( ! field.CompatibleUnit( new ForgeTypeId( unitTypeId ) ) )
                throw new ArgumentException( $"At field {field.FieldName}, {nameof( Autodesk.Revit.DB.UnitTypeId )} don't compatible with {nameof( Autodesk.Revit.DB.SpecTypeId )}." ) ;

            return true ;
        }

        #endregion
    }
}