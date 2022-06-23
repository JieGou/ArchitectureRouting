using System;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Arent3d.Utility.Serialization;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
    [StorableConverterOf(typeof(CnsSettingModel))]
    internal class CnsSettingModelStorableConverter : StorableConverterBase<CnsSettingModel>
    {
        private enum SerializeField
        {
            Sequence,
            CategoryName,
            IsDefaultItemChecked,
        }

        protected override CnsSettingModel Deserialize(Element storedElement, IDeserializerObject deserializerObject)
        {
            var deserializer = deserializerObject.Of<SerializeField>();

            int sequence = Convert.ToInt32(deserializer.GetInt(SerializeField.Sequence));
            var isDefaultItemChecked = deserializer.GetBool(SerializeField.IsDefaultItemChecked);
            string categoryName = deserializer.GetString(SerializeField.CategoryName)?.ToString() ?? string.Empty;

            return new CnsSettingModel(sequence, categoryName, isDefaultItemChecked ?? false);
        }

        protected override ISerializerObject Serialize(Element storedElement, CnsSettingModel customTypeValue)
        {
            var serializerObject = new SerializerObject<SerializeField>();

            serializerObject.Add(SerializeField.Sequence, customTypeValue.Sequence);
            serializerObject.Add(SerializeField.IsDefaultItemChecked, customTypeValue.IsDefaultItemChecked);
            serializerObject.AddNonNull(SerializeField.CategoryName, customTypeValue.CategoryName);
            
            return serializerObject;
        }
    }
}
