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
      IsCeedCodeNumber
    }
    
    protected override CategoryModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
      
      var name = deserializer.GetString( SerializeField.Name ) ;
      var parentName = deserializer.GetString( SerializeField.ParentName ) ;
      var isExpanded = deserializer.GetBool( SerializeField.IsExpanded ) ;
      var isSelected = deserializer.GetBool( SerializeField.IsSelected ) ;
      var isCeedCodeNumber = deserializer.GetBool( SerializeField.IsCeedCodeNumber ) ;

      return new CategoryModel( name, parentName, isExpanded, isSelected, isCeedCodeNumber ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, CategoryModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
      
      serializerObject.AddNonNull( SerializeField.Name, customTypeValue.Name ) ;
      serializerObject.AddNonNull( SerializeField.ParentName, customTypeValue.ParentName ) ;
      serializerObject.Add( SerializeField.IsExpanded, customTypeValue.IsExpanded ) ;
      serializerObject.Add( SerializeField.IsSelected, customTypeValue.IsSelected ) ;
      serializerObject.Add( SerializeField.IsCeedCodeNumber, customTypeValue.IsCeedCodeNumber ) ;

      return serializerObject ;
    }
  }
}