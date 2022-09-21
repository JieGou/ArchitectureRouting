using System.Linq ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
    [StorableConverterOf( typeof( ShaftOpeningModel ) )]
    public class ShaftOpeningStorableConverter : StorableConverterBase<ShaftOpeningModel>
    {
        private enum SerializeField
        {
            ShaftOpeningUniqueId,
            DetailUniqueIds,
            Size
        }

        protected override ShaftOpeningModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
        {
            var deserializer = deserializerObject.Of<SerializeField>() ;

            var shaftOpeningUniqueId = deserializer.GetString( SerializeField.ShaftOpeningUniqueId ) ;
            var detailUniqueIds = deserializer.GetNonNullStringArray( SerializeField.DetailUniqueIds ) ;
            var size = deserializer.GetDouble( SerializeField.Size ) ;

            return new ShaftOpeningModel( shaftOpeningUniqueId, detailUniqueIds?.ToList(), size ) ;
        }

        protected override ISerializerObject Serialize( Element storedElement, ShaftOpeningModel customTypeValue )
        {
            var serializerObject = new SerializerObject<SerializeField>() ;

            serializerObject.AddNonNull( SerializeField.ShaftOpeningUniqueId, customTypeValue.ShaftOpeningUniqueId ) ;
            serializerObject.AddNonNull( SerializeField.DetailUniqueIds, customTypeValue.DetailUniqueIds ) ;

            return serializerObject ;
        }
    }
}