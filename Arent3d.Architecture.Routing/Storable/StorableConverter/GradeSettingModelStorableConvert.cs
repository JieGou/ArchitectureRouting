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
      GradeMode
    }

    protected override GradeSettingModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var gradeMode = deserializer.GetInt( SerializeField.GradeMode ) ;

      return new GradeSettingModel( gradeMode! ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, GradeSettingModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.Add( SerializeField.GradeMode, customTypeValue.GradeMode ) ;

      return serializerObject ;
    }
  }
}