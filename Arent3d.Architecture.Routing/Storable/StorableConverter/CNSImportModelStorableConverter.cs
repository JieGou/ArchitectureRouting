using System;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Arent3d.Utility.Serialization;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
    [StorableConverterOf(typeof(CnsImportModel))]
    internal class CNSImportModelStorableConverter : StorableConverterBase<CnsImportModel>
    {
        private enum SerializeField
        {
            Senquen,
            CategoryName
        }

        protected override CnsImportModel Deserialize(Element storedElement, IDeserializerObject deserializerObject)
        {
            var deserializer = deserializerObject.Of<SerializeField>();

            int sequen = Convert.ToInt32(deserializer.GetInt(SerializeField.Senquen));
            string categoryName = deserializer.GetString(SerializeField.CategoryName)?.ToString() ?? string.Empty;

            return new CnsImportModel(sequen, categoryName);
        }

        protected override ISerializerObject Serialize(Element storedElement, CnsImportModel customTypeValue)
        {
            var serializerObject = new SerializerObject<SerializeField>();

            serializerObject.Add(SerializeField.Senquen, customTypeValue.Sequen);
            serializerObject.AddNonNull(SerializeField.CategoryName, customTypeValue.CategoryName);
            
            return serializerObject;
        }
    }
}