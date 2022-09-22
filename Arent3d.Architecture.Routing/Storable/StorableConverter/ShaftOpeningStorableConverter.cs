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
            ShaftIndex,
            ShaftOpeningUniqueId,
            CableTrayUniqueIds,
            DetailUniqueIds,
            Size
        }

        protected override ShaftOpeningModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
        {
            var deserializer = deserializerObject.Of<SerializeField>() ;

            var shaftIndex = deserializer.GetInt( SerializeField.ShaftIndex ) ;
            var shaftOpeningUniqueId = deserializer.GetString( SerializeField.ShaftOpeningUniqueId ) ;
            var cableTrayUniqueIds = deserializer.GetNonNullStringArray( SerializeField.CableTrayUniqueIds ) ;
            var detailUniqueIds = deserializer.GetNonNullStringArray( SerializeField.DetailUniqueIds ) ;
            var size = deserializer.GetDouble( SerializeField.Size ) ;

            return new ShaftOpeningModel( shaftIndex, shaftOpeningUniqueId, cableTrayUniqueIds?.ToList(), detailUniqueIds?.ToList(), size ) ;
        }

        protected override ISerializerObject Serialize( Element storedElement, ShaftOpeningModel customTypeValue )
        {
            var serializerObject = new SerializerObject<SerializeField>() ;

            serializerObject.Add( SerializeField.ShaftIndex, customTypeValue.ShaftIndex ) ;
            serializerObject.AddNonNull( SerializeField.ShaftOpeningUniqueId, customTypeValue.ShaftOpeningUniqueId ) ;
            serializerObject.AddNonNull( SerializeField.CableTrayUniqueIds, customTypeValue.CableTrayUniqueIds ) ;
            serializerObject.AddNonNull( SerializeField.DetailUniqueIds, customTypeValue.DetailUniqueIds ) ;
            serializerObject.Add( SerializeField.Size, customTypeValue.Size ) ;

            return serializerObject ;
        }
    }
}