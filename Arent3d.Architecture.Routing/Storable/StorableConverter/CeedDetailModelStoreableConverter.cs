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
      Order,
      Type,
      ModeNumber,
      CeedCode,
      ConstructionClassification,
      QuantityCalculate,
      QuantitySet,
      Total,
      Description,
      AllowInputQuantity,
      Supplement,
      IsConduit
    }

    protected override CeedDetailModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var productCode = deserializer.GetString( SerializeField.ProductCode ) ;
      var productName = deserializer.GetString( SerializeField.ProductName ) ;
      var standard = deserializer.GetString( SerializeField.Standard ) ;
      var classification = deserializer.GetString( SerializeField.Classification ) ;
      var quantity = deserializer.GetString( SerializeField.Quantity ) ;
      var unit = deserializer.GetString( SerializeField.Unit ) ; 
      var parentId = deserializer.GetString( SerializeField.ParentId ) ; 
      var trajectory = deserializer.GetString( SerializeField.Trajectory ) ; 
      var size1 = deserializer.GetString( SerializeField.Size1 ) ; 
      var size2 = deserializer.GetString( SerializeField.Size2 ) ; 
      var specification = deserializer.GetString( SerializeField.Specification ) ; 
      var type = deserializer.GetString( SerializeField.Type ) ; 
      var order = deserializer.GetInt( SerializeField.Order ) ; 
      var ceedCode = deserializer.GetString( SerializeField.CeedCode ) ; 
      var constructionClassification = deserializer.GetString( SerializeField.ConstructionClassification ) ; 
      var quantityCalculate = deserializer.GetDouble( SerializeField.QuantityCalculate ) ; 
      var quantitySet = deserializer.GetDouble( SerializeField.QuantitySet ) ; 
      var total = deserializer.GetDouble( SerializeField.Total ) ;  
      var modeNumber = deserializer.GetString( SerializeField.ModeNumber ) ; 
      var description = deserializer.GetString( SerializeField.Description ) ; 
      var allowInputQuantity = deserializer.GetBool( SerializeField.AllowInputQuantity ) ;
      var supplement = deserializer.GetString( SerializeField.Supplement ) ; 
      var isConduit = deserializer.GetBool( SerializeField.IsConduit ) ?? false ;

      return new CeedDetailModel(productCode, productName, standard, classification, quantity, unit, parentId, trajectory, size1, size2, specification, order, type, string.Empty, modeNumber, ceedCode, constructionClassification, quantityCalculate, quantitySet, total, description, allowInputQuantity, supplement, isConduit ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, CeedDetailModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.ProductCode, customTypeValue.ProductCode ) ;
      serializerObject.AddNonNull( SerializeField.ProductName, customTypeValue.ProductName ) ;
      serializerObject.AddNonNull( SerializeField.Standard, customTypeValue.Standard ) ;
      serializerObject.AddNonNull( SerializeField.Classification, customTypeValue.Classification ) ;
      serializerObject.AddNonNull( SerializeField.Quantity, customTypeValue.Quantity ) ;
      serializerObject.AddNonNull( SerializeField.Unit, customTypeValue.Unit ) ;
      serializerObject.AddNonNull( SerializeField.ParentId, customTypeValue.ParentId ) ;
      serializerObject.AddNonNull( SerializeField.Trajectory, customTypeValue.Trajectory ) ;
      serializerObject.AddNonNull( SerializeField.Size1, customTypeValue.Size1 ) ;
      serializerObject.AddNonNull( SerializeField.Size2, customTypeValue.Size2 ) ;
      serializerObject.AddNonNull( SerializeField.Specification, customTypeValue.Specification ) ;
      serializerObject.AddNonNull( SerializeField.Type, customTypeValue.Type ) ;
      serializerObject.AddNonNull( SerializeField.ModeNumber, customTypeValue.ModeNumber ) ;
      serializerObject.Add( SerializeField.Order, customTypeValue.Order ) ;
      serializerObject.AddNonNull( SerializeField.ConstructionClassification, customTypeValue.ConstructionClassification ) ;
      serializerObject.AddNonNull( SerializeField.CeedCode, customTypeValue.CeedCode ) ;
      serializerObject.Add( SerializeField.QuantityCalculate, customTypeValue.QuantityCalculate ) ;
      serializerObject.Add( SerializeField.QuantitySet, customTypeValue.QuantitySet ) ;
      serializerObject.Add( SerializeField.Total, customTypeValue.Total ) ;
      serializerObject.AddNonNull( SerializeField.Description, customTypeValue.Description ) ;
      serializerObject.Add( SerializeField.AllowInputQuantity, customTypeValue.AllowInputQuantity ) ;
      serializerObject.AddNonNull( SerializeField.Supplement, customTypeValue.Supplement ) ;
      serializerObject.Add( SerializeField.IsConduit, customTypeValue.IsConduit ) ;
        
      return serializerObject ;
    }
  }
}