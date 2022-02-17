using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( DetailTableModel ) )]
  public class DetailTableModelStorableConvert : StorableConverterBase<DetailTableModel>
  {
    private enum SerializeField
    {
      CalculationExclusion,
      Floor,
      CeeDCode,
      DetailSymbol,
      DetailSymbolId,
      WireType,
      WireSize,
      WireStrip,
      WireBook,
      EarthType,
      EarthSize,
      NumberOfGrounds,
      PlumbingType,
      PlumbingSize,
      NumberOfPlumbing,
      ConstructionClassification,
      SignalType,
      ConstructionItems,
      PlumbingItems,
      Remark,
      WireCrossSectionalArea,
      CountCableSamePosition,
      RouteName,
      IsEcoMode,
      IsParentRoute,
      IsReadOnly,
      ParentPlumbingType,
      GroupId
    }

    protected override DetailTableModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var calculationExclusion = deserializer.GetBool( SerializeField.CalculationExclusion ) ;
      var floor = deserializer.GetString( SerializeField.Floor ) ;
      var ceeDCode = deserializer.GetString( SerializeField.CeeDCode ) ;
      var detailSymbolId = deserializer.GetString( SerializeField.DetailSymbolId ) ;
      var detailSymbol = deserializer.GetString( SerializeField.DetailSymbol ) ;
      var wireType = deserializer.GetString( SerializeField.WireType ) ;
      var wireSize = deserializer.GetString( SerializeField.WireSize ) ;
      var wireStrip = deserializer.GetString( SerializeField.WireStrip ) ;
      var wireBook = deserializer.GetString( SerializeField.WireBook ) ;
      var earthType = deserializer.GetString( SerializeField.EarthType ) ;
      var earthSize = deserializer.GetString( SerializeField.EarthSize ) ;
      var numberOfGrounds = deserializer.GetString( SerializeField.NumberOfGrounds ) ;
      var plumbingType = deserializer.GetString( SerializeField.PlumbingType ) ;
      var plumbingSize = deserializer.GetString( SerializeField.PlumbingSize ) ;
      var numberOfPlumbing = deserializer.GetString( SerializeField.NumberOfPlumbing ) ;
      var constructionClassification = deserializer.GetString( SerializeField.ConstructionClassification ) ;
      var signalType = deserializer.GetString( SerializeField.SignalType ) ;
      var constructionItems = deserializer.GetString( SerializeField.ConstructionItems ) ;
      var plumbingItems = deserializer.GetString( SerializeField.PlumbingItems ) ;
      var remark = deserializer.GetString( SerializeField.Remark ) ;
      var wireCrossSectionalArea = deserializer.GetDouble( SerializeField.WireCrossSectionalArea ) ;
      var countCableSamePosition = deserializer.GetInt( SerializeField.CountCableSamePosition ) ;
      var routeName = deserializer.GetString( SerializeField.RouteName ) ;
      var isEcoMode = deserializer.GetString( SerializeField.IsEcoMode ) ;
      var isParentRoute = deserializer.GetBool( SerializeField.IsParentRoute ) ;
      var isReadOnly = deserializer.GetBool( SerializeField.IsReadOnly ) ;
      var parentPlumbingType = deserializer.GetString( SerializeField.ParentPlumbingType ) ;
      var groupId = deserializer.GetString( SerializeField.GroupId ) ;

      return new DetailTableModel( calculationExclusion, floor, ceeDCode, detailSymbol, detailSymbolId, wireType, wireSize, wireStrip, wireBook, earthType, earthSize, numberOfGrounds, plumbingType, plumbingSize, numberOfPlumbing, constructionClassification, signalType, constructionItems, plumbingItems, remark, wireCrossSectionalArea, countCableSamePosition, routeName, isEcoMode, isParentRoute, isReadOnly, parentPlumbingType, groupId ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, DetailTableModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.Add( SerializeField.CalculationExclusion, customTypeValue.CalculationExclusion ) ;
      serializerObject.AddNonNull( SerializeField.Floor, customTypeValue.Floor ) ;
      serializerObject.AddNonNull( SerializeField.CeeDCode, customTypeValue.CeeDCode ) ;
      serializerObject.AddNonNull( SerializeField.DetailSymbolId, customTypeValue.DetailSymbolId ) ;
      serializerObject.AddNonNull( SerializeField.DetailSymbol, customTypeValue.DetailSymbol ) ;
      serializerObject.AddNonNull( SerializeField.WireType, customTypeValue.WireType ) ;
      serializerObject.AddNonNull( SerializeField.WireSize, customTypeValue.WireSize ) ;
      serializerObject.AddNonNull( SerializeField.WireStrip, customTypeValue.WireStrip ) ;
      serializerObject.AddNonNull( SerializeField.WireBook, customTypeValue.WireBook ) ;
      serializerObject.AddNonNull( SerializeField.EarthType, customTypeValue.EarthType ) ;
      serializerObject.AddNonNull( SerializeField.EarthSize, customTypeValue.EarthSize ) ;
      serializerObject.AddNonNull( SerializeField.NumberOfGrounds, customTypeValue.NumberOfGrounds ) ;
      serializerObject.AddNonNull( SerializeField.PlumbingType, customTypeValue.PlumbingType ) ;
      serializerObject.AddNonNull( SerializeField.PlumbingSize, customTypeValue.PlumbingSize ) ;
      serializerObject.AddNonNull( SerializeField.NumberOfPlumbing, customTypeValue.NumberOfPlumbing ) ;
      serializerObject.AddNonNull( SerializeField.ConstructionClassification, customTypeValue.ConstructionClassification ) ;
      serializerObject.AddNonNull( SerializeField.SignalType, customTypeValue.SignalType ) ;
      serializerObject.AddNonNull( SerializeField.ConstructionItems, customTypeValue.ConstructionItems ) ;
      serializerObject.AddNonNull( SerializeField.PlumbingItems, customTypeValue.PlumbingItems ) ;
      serializerObject.AddNonNull( SerializeField.Remark, customTypeValue.Remark ) ;
      serializerObject.Add( SerializeField.WireCrossSectionalArea, customTypeValue.WireCrossSectionalArea ) ;
      serializerObject.Add( SerializeField.CountCableSamePosition, customTypeValue.CountCableSamePosition ) ;
      serializerObject.AddNonNull( SerializeField.RouteName, customTypeValue.RouteName ) ;
      serializerObject.AddNonNull( SerializeField.IsEcoMode, customTypeValue.IsEcoMode ) ;
      serializerObject.Add( SerializeField.IsParentRoute, customTypeValue.IsParentRoute ) ;
      serializerObject.Add( SerializeField.IsReadOnly, customTypeValue.IsReadOnly ) ;
      serializerObject.AddNonNull( SerializeField.ParentPlumbingType, customTypeValue.ParentPlumbingType ) ;
      serializerObject.AddNonNull( SerializeField.GroupId, customTypeValue.GroupId ) ;

      return serializerObject ;
    }
  }
}