using System ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages.Extensions
{
  public static class EntityElementExtension
  {
    public static void SetEntity( this Element element, IModelEntity modelEntity )
    {
      ISchemaCreator schemaCreator = new SchemaCreator() ;
      IEntityConverter entityConverter = new EntityConverter( schemaCreator ) ;
      var entity = entityConverter.Convert( modelEntity ) ;
      element.SetEntity( entity ) ;
    }

    public static TModelEntity? GetEntity<TModelEntity>( this Element element ) where TModelEntity : class, IModelEntity
    {
      var modelEntityType = typeof( TModelEntity ) ;
      var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
      var schemaAttribute = schemaAttributeExtractor.GetAttribute( modelEntityType ) ;

      if ( Schema.Lookup( schemaAttribute.GUID ) is not { } schema )
        return null ;

      var entity = element.GetEntity( schema ) ;
      if ( entity == null || ! entity.IsValid() )
        return null ;

      ISchemaCreator schemaCreator = new SchemaCreator() ;
      IEntityConverter entityConverter = new EntityConverter( schemaCreator ) ;

      var modelEntity = entityConverter.Convert<TModelEntity>( entity ) ;
      return modelEntity ;
    }

    public static bool DeleteEntity<TModelEntity>( this Element element ) where TModelEntity : class, IModelEntity
    {
      var modelType = typeof( TModelEntity ) ; 
      var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
      var schemaAttribute = schemaAttributeExtractor.GetAttribute( modelType ) ;

      return Schema.Lookup( schemaAttribute.GUID ) is { } schema && element.DeleteEntity( schema ) ;
    }
  }
}