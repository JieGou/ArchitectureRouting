using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( LimitRackModel ) )]
  public class LimitRackStorableConverter : StorableConverterBase<LimitRackModel>
  {
    private enum SerializeField
    {
      RackIds,
      RackDetailCurveIds
    }


    protected override LimitRackModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var rackIds = deserializer.GetNonNullStringArray( SerializeField.RackIds )?.ToList() ;
      rackIds = rackIds == null ? new List<string>() : rackIds.ToList() ;
      var rackDetailCurveIds = deserializer.GetNonNullStringArray( SerializeField.RackDetailCurveIds )?.ToList() ;
      rackDetailCurveIds = rackDetailCurveIds == null ? new List<string>() : rackDetailCurveIds.ToList() ;

      return new LimitRackModel( rackIds, rackDetailCurveIds ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, LimitRackModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.RackIds, customTypeValue.RackIds ) ;
      serializerObject.AddNonNull( SerializeField.RackDetailCurveIds, customTypeValue.RackDetailLineIds ) ;

      return serializerObject ;
    }
  }
}