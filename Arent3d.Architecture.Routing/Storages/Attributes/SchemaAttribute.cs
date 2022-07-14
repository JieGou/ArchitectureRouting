using System ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storages.Attributes
{
    /// <summary>
    /// Definition of a schema
    /// </summary>
    [AttributeUsage( AttributeTargets.Class )]
    public class SchemaAttribute : Attribute
    {
        public SchemaAttribute( string guid, string schemaName )
        {
            if ( ! Guid.TryParse( guid, out var value ) )
                throw new InvalidCastException( "GUID value is not in the correct format." ) ;

            GUID = value ;
            SchemaName = schemaName ;
        }

        /*
         * Change the name or data type of the existing property or add a new property to the model, please change the model's GUID 
         * and remove the schema and all entities with the old GUID. Otherwise, there will be a memory leak.
         * Steps:
         * 1. Schema.Lookup(Guid guid).
         * 2. Document.EraseSchemaAndAllEntities(Schema schema).
         */
        public Guid GUID { get ; }
        public string SchemaName { get ; }


        public Guid ApplicationGUID { get ; set ; } = Guid.Empty ;

        public string Documentation { get ; set ; } = string.Empty ;

        public AccessLevel ReadAccessLevel { get ; set ; } = AccessLevel.Vendor ;

        public AccessLevel WriteAccessLevel { get ; set ; } = AccessLevel.Vendor ;

        public string VendorId { get ; set ; } = AppInfo.VendorId ;
    }
}