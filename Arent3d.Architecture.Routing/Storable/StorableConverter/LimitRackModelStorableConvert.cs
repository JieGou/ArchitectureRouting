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
      LimitRackIds,
      LimitRackFittingIds,
      LimitRackDetailIds
    }

    protected override LimitRackModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var limitRackIds = deserializer.GetNonNullStringArray( SerializeField.LimitRackIds).ToList() ;
      var limitRackFittingIds = deserializer.GetNonNullStringArray( SerializeField.LimitRackFittingIds).ToList() ;
      var limitRackDetailIds = deserializer.GetNonNullStringArray( SerializeField.LimitRackDetailIds ).ToList() ;
      return new LimitRackModel( )
      {
        LimitRackIds = limitRackIds,
        LitmitRackFittingIds = limitRackFittingIds,
        LimitRackDetailIds = limitRackDetailIds
        
      };
    }

    protected override ISerializerObject Serialize( Element storedElement, LimitRackModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
      serializerObject.AddNonNull( SerializeField.LimitRackIds, customTypeValue.LimitRackIds ) ;
      serializerObject.AddNonNull( SerializeField.LimitRackFittingIds, customTypeValue.LitmitRackFittingIds ) ;
      serializerObject.AddNonNull( SerializeField.LimitRackDetailIds, customTypeValue.LimitRackDetailIds ) ;
      return serializerObject ;
    }
  }
}