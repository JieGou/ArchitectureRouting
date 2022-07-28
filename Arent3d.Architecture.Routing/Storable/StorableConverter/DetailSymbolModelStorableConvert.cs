using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( DetailSymbolModel ) )]
  public class DetailSymbolModelStorableConvert : StorableConverterBase<DetailSymbolModel>
  {
    private enum SerializeField
    {
      DetailSymbol,
      DetailSymbolUniqueId,
      FromConnectorUniqueId,
      ToConnectorUniqueId,
      ConduitId,
      RouteName,
      Code,
      LineIds,
      IsParentSymbol,
      CountCableSamePosition, 
      DeviceSymbol,
      PlumbingType
    }

    protected override DetailSymbolModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var detailSymbol = deserializer.GetString( SerializeField.DetailSymbol ) ;
      var detailSymbolId = deserializer.GetString( SerializeField.DetailSymbolUniqueId ) ;
      var fromConnectorUniqueId = deserializer.GetString( SerializeField.FromConnectorUniqueId ) ;
      var toConnectorUniqueId = deserializer.GetString( SerializeField.ToConnectorUniqueId ) ;
      var conduitId = deserializer.GetString( SerializeField.ConduitId ) ;
      var routeName = deserializer.GetString( SerializeField.RouteName ) ;
      var code = deserializer.GetString( SerializeField.Code ) ;
      var lineIds = deserializer.GetString( SerializeField.LineIds ) ;
      var isParentSymbol = deserializer.GetBool( SerializeField.IsParentSymbol ) ;
      var countCableSamePosition = deserializer.GetInt( SerializeField.CountCableSamePosition ) ;
      var deviceSymbol = deserializer.GetString( SerializeField.DeviceSymbol ) ;
      var plumbingType = deserializer.GetString( SerializeField.PlumbingType ) ;

      return new DetailSymbolModel( detailSymbol, detailSymbolId, fromConnectorUniqueId, toConnectorUniqueId, conduitId, routeName, code, lineIds, isParentSymbol, countCableSamePosition, deviceSymbol, plumbingType ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, DetailSymbolModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.DetailSymbol, customTypeValue.DetailSymbol ) ;
      serializerObject.AddNonNull( SerializeField.DetailSymbolUniqueId, customTypeValue.DetailSymbolUniqueId ) ;
      serializerObject.AddNonNull( SerializeField.FromConnectorUniqueId, customTypeValue.FromConnectorUniqueId ) ;
      serializerObject.AddNonNull( SerializeField.ToConnectorUniqueId, customTypeValue.ToConnectorUniqueId ) ;
      serializerObject.AddNonNull( SerializeField.ConduitId, customTypeValue.ConduitId ) ;
      serializerObject.AddNonNull( SerializeField.RouteName, customTypeValue.RouteName ) ;
      serializerObject.AddNonNull( SerializeField.Code, customTypeValue.Code ) ;
      serializerObject.AddNonNull( SerializeField.LineIds, customTypeValue.LineIds ) ;
      serializerObject.Add( SerializeField.IsParentSymbol, customTypeValue.IsParentSymbol ) ;
      serializerObject.Add( SerializeField.CountCableSamePosition, customTypeValue.CountCableSamePosition ) ;
      serializerObject.AddNonNull( SerializeField.DeviceSymbol, customTypeValue.DeviceSymbol ) ;
      serializerObject.AddNonNull( SerializeField.PlumbingType, customTypeValue.PlumbingType ) ;

      return serializerObject ;
    }
  }
}