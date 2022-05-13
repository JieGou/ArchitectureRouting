using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( GradeSettingModel ) )]
  public class GradeSettingModelStorableConvert : StorableConverterBase<GradeSettingModel>
  {
    private enum SerializeField
    {
      IsInGrade3Mode
    }

    protected override GradeSettingModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var isInGrade3Mode = deserializer.GetBool( SerializeField.IsInGrade3Mode ) ;

      return new GradeSettingModel( isInGrade3Mode! ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, GradeSettingModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.Add( SerializeField.IsInGrade3Mode, customTypeValue.IsInGrade3Mode ) ;

      return serializerObject ;
    }
  }
}