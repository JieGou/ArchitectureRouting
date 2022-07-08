using System.Linq ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( LimitRackModel ) )]
  public class LimitRackModelStorableConvert : StorableConverterBase<LimitRackModel>
  {
    private enum SerializeField
    {
      RouteName,
      RackIds
    }

    protected override LimitRackModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var routeName = deserializer.GetString( SerializeField.RouteName ) ;
      var rackIds = deserializer.GetNonNullStringArray( SerializeField.RackIds ).ToList() ;
      var limitRack = new LimitRackModel( routeName ) ;
      limitRack.RackIds.AddRange( rackIds ) ;
      return limitRack ;
    }

    protected override ISerializerObject Serialize( Element storedElement, LimitRackModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
      serializerObject.AddNonNull( SerializeField.RouteName, customTypeValue.RouteName );
      serializerObject.AddNonNull( SerializeField.RackIds , customTypeValue.RackIds );
      return serializerObject ;
    }
  }
}