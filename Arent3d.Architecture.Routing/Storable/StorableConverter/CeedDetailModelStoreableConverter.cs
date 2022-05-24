using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( CeedDetailModel ) )]
  public class CeedDetailModelStoreableConverter : StorableConverterBase<CeedDetailModel>
  {
    private enum SerializeField
    {
      ProductCode,
      ProductName,
      Standard,
      Classification,
      Quantity,
      Unit,
      ParentId,
      Trajectory,
      Size1,
      Size2,
      Specification,
    }

    protected override CeedDetailModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var productCode = deserializer.GetString( SerializeField.ProductCode ) ;
      var productName = deserializer.GetString( SerializeField.ProductName ) ;
      var standard = deserializer.GetString( SerializeField.Standard ) ;
      var classification = deserializer.GetString( SerializeField.Classification ) ;
      var quantity = deserializer.GetDouble( SerializeField.Quantity ) ;
      var unit = deserializer.GetString( SerializeField.Unit ) ; 
      var parentId = deserializer.GetString( SerializeField.ParentId ) ; 
      var trajectory = deserializer.GetString( SerializeField.Trajectory ) ; 
      var size1 = deserializer.GetString( SerializeField.Size1 ) ; 
      var size2 = deserializer.GetString( SerializeField.Size2 ) ; 
      var specification = deserializer.GetString( SerializeField.Specification ) ; 

      return new CeedDetailModel(productCode, productName, standard, classification, quantity, unit, parentId, trajectory, size1, size2, specification) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, CeedDetailModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.ProductCode, customTypeValue.ProductCode ) ;
      serializerObject.AddNonNull( SerializeField.ProductName, customTypeValue.ProductName ) ;
      serializerObject.AddNonNull( SerializeField.Standard, customTypeValue.Standard ) ;
      serializerObject.AddNonNull( SerializeField.Classification, customTypeValue.Classification ) ;
      serializerObject.Add( SerializeField.Quantity, customTypeValue.Quantity ) ;
      serializerObject.AddNonNull( SerializeField.Unit, customTypeValue.Unit ) ;
      serializerObject.AddNonNull( SerializeField.ParentId, customTypeValue.ParentId ) ;
      serializerObject.AddNonNull( SerializeField.Trajectory, customTypeValue.Trajectory ) ;
      serializerObject.AddNonNull( SerializeField.Size1, customTypeValue.Size1 ) ;
      serializerObject.AddNonNull( SerializeField.Size2, customTypeValue.Size2 ) ;
      serializerObject.AddNonNull( SerializeField.Specification, customTypeValue.Specification ) ;
        
      return serializerObject ;
    }
  }
}