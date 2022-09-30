using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( CategoryModel ) )]
  public class CategoryModelStorableConvert : StorableConverterBase<CategoryModel>
  {
    private enum SerializeField
    {
      Name,
      ParentName,
      IsExpanded,
      IsSelected,
      IsCeedCodeNumber,
      IsExistModelNumber,
      IsMainConstruction,
      IsPower
    }
    
    protected override CategoryModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
      
      var name = deserializer.GetString( SerializeField.Name ) ;
      var parentName = deserializer.GetString( SerializeField.ParentName ) ;
      var isExpanded = deserializer.GetBool( SerializeField.IsExpanded ) ;
      var isSelected = deserializer.GetBool( SerializeField.IsSelected ) ;
      var isCeedCodeNumber = deserializer.GetBool( SerializeField.IsCeedCodeNumber ) ;
      var isExistModelNumber = deserializer.GetBool( SerializeField.IsExistModelNumber ) ;
      var isMainConstruction = deserializer.GetBool( SerializeField.IsMainConstruction ) ;
      var isPower = deserializer.GetBool( SerializeField.IsPower ) ;

      return new CategoryModel( name, parentName, isExpanded, isSelected, isCeedCodeNumber, isExistModelNumber, isMainConstruction, isPower ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, CategoryModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
      
      serializerObject.AddNonNull( SerializeField.Name, customTypeValue.Name ) ;
      serializerObject.AddNonNull( SerializeField.ParentName, customTypeValue.ParentName ) ;
      serializerObject.Add( SerializeField.IsExpanded, customTypeValue.IsExpanded ) ;
      serializerObject.Add( SerializeField.IsSelected, customTypeValue.IsSelected ) ;
      serializerObject.Add( SerializeField.IsCeedCodeNumber, customTypeValue.IsCeedCodeNumber ) ;
      serializerObject.Add( SerializeField.IsExistModelNumber, customTypeValue.IsExistModelNumber ) ;
      serializerObject.Add( SerializeField.IsMainConstruction, customTypeValue.IsMainConstruction ) ;
      serializerObject.Add( SerializeField.IsPower, customTypeValue.IsPower ) ;

      return serializerObject ;
    }
  }
}