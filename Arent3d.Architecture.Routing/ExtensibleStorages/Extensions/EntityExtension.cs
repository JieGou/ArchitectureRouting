using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages.Extensions
{
    public static class EntityExtension
    {
        public static void SetWrapper<T>( this Entity entity, Field field, IList<T> value )
        {
            entity.Set( field, value ) ;
        }

        public static void SetWrapper<T>( this Entity entity, Field field, IList<T> value, ForgeTypeId unitTypeId )
        {
            entity.Set( field, value, unitTypeId ) ;
        }

        public static void SetWrapper<TKey, TValue>( this Entity entity, Field field, IDictionary<TKey, TValue> value )
        {
            entity.Set( field, value ) ;
        }

        public static void SetWrapper<TKey, TValue>( this Entity entity, Field field, IDictionary<TKey, TValue> value, ForgeTypeId unitTypeId )
        {
            entity.Set( field, value, unitTypeId ) ;
        }

        public static void SetData( this Element element, IDataModel dataModel )
        {
            ISchemaCreator schemaCreator = new SchemaCreator() ;
            IEntityConverter entityConverter = new EntityConverter( schemaCreator ) ;
            var entity = entityConverter.Convert( dataModel ) ;
            element.SetEntity( entity ) ;
        }

        public static TDataModel? GetData<TDataModel>( this Element element ) where TDataModel : class, IDataModel
        {
            var dataModelType = typeof( TDataModel ) ;
            var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
            var schemaAttribute = schemaAttributeExtractor.GetAttribute( dataModelType ) ;

            var schema = Schema.Lookup( schemaAttribute.GUID ) ;
            if ( schema is null )
                return null ;

            var entity = element.GetEntity( schema ) ;
            if ( entity == null || ! entity.IsValid() )
                return null ;

            ISchemaCreator schemaCreator = new SchemaCreator() ;
            IEntityConverter entityConverter = new EntityConverter( schemaCreator ) ;

            var dataModel = entityConverter.Convert<TDataModel>( entity ) ;
            return dataModel ;
        }

        public static bool DeleteData<TDataModel>( this Element element ) where TDataModel : class, IDataModel
        {
            var dataModelType = typeof( TDataModel ) ;
            var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
            var schemaAttribute = schemaAttributeExtractor.GetAttribute( dataModelType ) ;

            return Schema.Lookup( schemaAttribute.GUID ) is { } schema && element.DeleteEntity( schema ) ;
        }

        public static bool IsAcceptValueType( this Type type )
        {
            return ValueTypeAccepts.Any( x => x == type ) ;
        }

        public static bool IsAcceptKeyType( this Type type )
        {
            return KeyTypeAccepts.Any( x => x == type ) ;
        }

        private static HashSet<Type> ValueTypeAccepts => new(new List<Type>
            {
                typeof( int ),
                typeof( short ),
                typeof( byte ),
                typeof( double ),
                typeof( float ),
                typeof( bool ),
                typeof( string ),
                typeof( Guid ),
                typeof( ElementId ),
                typeof( XYZ ),
                typeof( UV ),
                typeof(Entity)
            }) ;

        private static HashSet<Type> KeyTypeAccepts => new(new List<Type>
            {
                typeof( int ),
                typeof( short ),
                typeof( byte ),
                typeof( bool ),
                typeof( string ),
                typeof( Guid ),
                typeof( ElementId )
            }) ;
    }
}