using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storages.Extensions
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

        /// <summary>
        /// Find or create DataStorage
        /// </summary>
        /// <param name="document">Document</param>
        /// <param name="isForUser">True - Each user owns a DataStorage. False - All users share a DataStorage</param>
        /// <typeparam name="TDataModel">A class that inherits from IDataModel</typeparam>
        /// <returns>DataStorage</returns>
        public static DataStorage FindOrCreateDataStorage<TDataModel>( this Document document, bool isForUser ) where TDataModel : class, IDataModel
        {
            if ( default == document )
                throw new ArgumentNullException( nameof( document ) ) ;

            string dataStorageName ;
            if ( isForUser ) {
                if ( string.IsNullOrEmpty( document.Application.LoginUserId ) )
                    throw new InvalidOperationException( "Please login to Revit." ) ;

                dataStorageName = $"{AppInfo.VendorId}-{document.Application.Username}-{document.Application.LoginUserId}" ;
            }
            else {
                var dataModelType = typeof( TDataModel ) ;
                var schemaAttribute = dataModelType.GetAttribute<SchemaAttribute>() ;

                dataStorageName = $"{AppInfo.VendorId}-{schemaAttribute.GUID}" ;
            }


            DataStorage? dataStorage ;
            if ( isForUser ) {
                dataStorage = document.GetAllInstances<DataStorage>().SingleOrDefault( x => x.Name.StartsWith( AppInfo.VendorId ) && x.Name.EndsWith( document.Application.LoginUserId ) ) ;
            }
            else {
                dataStorage = document.GetAllInstances<DataStorage>().SingleOrDefault( x => x.Name == dataStorageName ) ;
            }

            if ( null != dataStorage )
                return dataStorage ;

            using var transaction = new Transaction( document ) ;
            transaction.OpenTransactionIfNeed( document, "Create Storage", () => { dataStorage = document.CreateDataStorage( dataStorageName ) ; } ) ;

            return dataStorage! ;
        }

        public static DataStorage CreateDataStorage( this Document document, string dataStorageName )
        {
            if ( default == document )
                throw new ArgumentNullException( nameof( document ) ) ;

            var dataStorage = DataStorage.Create( document ) ;
            dataStorage.Name = dataStorageName ;
            return dataStorage ;
        }

        public static IEnumerable<(TOwner Owner, TDataModel Data)> GetAllDatas<TOwner, TDataModel>( this Document document ) where TOwner : Element where TDataModel : class, IDataModel
        {
            if ( default == document )
                throw new ArgumentNullException( nameof( document ) ) ;

            var datas = new List<(TOwner Owner, TDataModel Data)>() ;

            var owners = document.GetAllInstances<TOwner>( o => o is not DataStorage dataStorage || dataStorage.Name.StartsWith( AppInfo.VendorId ) ) ;
            foreach ( var owner in owners ) {
                if ( owner.GetData<TDataModel>() is not { } data )
                    continue ;

                datas.Add( ( owner, data ) ) ;
            }

            return datas ;
        }

        public static void DeleteSchema<TDataModel>( this Document document ) where TDataModel : class, IDataModel
        {
            if ( default == document )
                throw new ArgumentNullException( nameof( document ) ) ;

            var schemaAttribute = typeof( TDataModel ).GetAttribute<SchemaAttribute>() ;
            if ( Schema.Lookup( schemaAttribute.GUID ) is not { } schema )
                return ;

            using var transaction = new Transaction( document ) ;
            transaction.OpenTransactionIfNeed( document, "Delete Schema", () =>
            {
                if ( schema.VendorId == AppInfo.VendorId.ToUpper() )
                    document.EraseSchemaAndAllEntities( schema ) ;
            } ) ;
        }

        public static void DeleteEntireSchema( this Document document )
        {
            if ( default == document )
                throw new ArgumentNullException( nameof( document ) ) ;

            var schemas = Schema.ListSchemas() ;
            if ( ! schemas.Any() )
                return ;

            using var transaction = new Transaction( document ) ;
            transaction.OpenTransactionIfNeed( document, "Delete Entire Schema", () =>
            {
                foreach ( var schema in schemas ) {
                    if ( schema.VendorId == AppInfo.VendorId.ToUpper() )
                        document.EraseSchemaAndAllEntities( schema ) ;
                }
            } ) ;
        }

        public static void SetData( this Element element, IDataModel dataModel )
        {
            if ( default == element )
                throw new ArgumentNullException( nameof( element ) ) ;

            ISchemaCreator schemaCreator = new SchemaCreator() ;
            IEntityConverter entityConverter = new EntityConverter( schemaCreator ) ;
            var entity = entityConverter.Convert( dataModel ) ;

            using var transaction = new Transaction( element.Document ) ;
            transaction.OpenTransactionIfNeed( element.Document, "Set Data", () => { element.SetEntity( entity ) ; } ) ;
        }

        public static TDataModel? GetData<TDataModel>( this Element element ) where TDataModel : class, IDataModel
        {
            if ( default == element )
                throw new ArgumentNullException( nameof( element ) ) ;

            var dataModelType = typeof( TDataModel ) ;
            var schemaAttribute = dataModelType.GetAttribute<SchemaAttribute>() ;

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

        public static void DeleteData<TDataModel>( this Element element ) where TDataModel : class, IDataModel
        {
            if ( default == element )
                throw new ArgumentNullException( nameof( element ) ) ;

            var schemaAttribute = typeof( TDataModel ).GetAttribute<SchemaAttribute>() ;
            if ( Schema.Lookup( schemaAttribute.GUID ) is not { } schema )
                return ;

            using var transaction = new Transaction( element.Document ) ;
            transaction.OpenTransactionIfNeed( element.Document, "Delete Data", () => { element.DeleteEntity( schema ) ; } ) ;
        }

        public static void DeleteEntireDataOnElement( this Element element )
        {
            if ( default == element )
                throw new ArgumentNullException( nameof( element ) ) ;

            var schemaIds = element.GetEntitySchemaGuids() ;
            if ( ! schemaIds.Any() )
                return ;

            using var transaction = new Transaction( element.Document ) ;
            transaction.OpenTransactionIfNeed( element.Document, "Delete Entire Data On Element", () =>
            {
                foreach ( var schemaId in schemaIds ) {
                    if ( Schema.Lookup( schemaId ) is not { } schema )
                        continue ;

                    element.DeleteEntity( schema ) ;
                }
            } ) ;
        }

        public static bool IsAcceptValueType( this Type type ) => ValueTypeAccepts.Any( x => x == type ) ;

        public static bool IsAcceptKeyType( this Type type ) => KeyTypeAccepts.Any( x => x == type ) ;

        public static bool IsFloatingPoint( this Type type ) => FloatingPointTypes.Any( x => x == type ) ;

        private static HashSet<Type> ValueTypeAccepts =>
            new(new List<Type>
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
                typeof( Entity )
            }) ;

        private static HashSet<Type> KeyTypeAccepts =>
            new(new List<Type>
            {
                typeof( int ),
                typeof( short ),
                typeof( byte ),
                typeof( bool ),
                typeof( string ),
                typeof( Guid ),
                typeof( ElementId )
            }) ;

        private static HashSet<Type> FloatingPointTypes => 
            new(new List<Type>
            {
                typeof( double ), 
                typeof( float ), 
                typeof( XYZ ), 
                typeof( UV )
            }) ;
    }
}