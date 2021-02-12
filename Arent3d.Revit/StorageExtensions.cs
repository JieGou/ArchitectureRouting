using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Reflection ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit
{
  public static class StorageExtensions
  {
    public static IEnumerable<TStorableBase> GetAllStorables<TStorableBase>( this Document document ) where TStorableBase : StorableBase
    {
      if ( Schema.Lookup( typeof( TStorableBase ).GUID ) is not { } schema ) yield break ;

      foreach ( var element in document.GetAllElements<Element>().Where( new ExtensibleStorageFilter( typeof( TStorableBase ).GUID ) ) ) {
        var entity = element.GetEntity( schema ) ;
        if ( null == entity || false == entity.IsValidObject || false == entity.IsValid() ) continue ;

        yield return CreateFromEntity<TStorableBase>( entity, element ) ;
      }
    }
    public static TStorableBase? GetStorable<TStorableBase>( this Element element ) where TStorableBase : StorableBase
    {
      if ( Schema.Lookup( typeof( TStorableBase ).GUID ) is not { } schema ) return null ;

      var entity = element.GetEntity( schema ) ;
      if ( null == entity || false == entity.IsValidObject || false == entity.IsValid() ) return null ;

      return CreateFromEntity<TStorableBase>( entity, element ) ;
    }

    internal static void SaveStorable( this Element element, StorableBase storable )
    {
      element.SetEntity( CreateEntity( element, storable ) ) ;
    }

    internal static void DeleteStorable( this Element element, StorableBase storable )
    {
      var schema = MakeCertainSchema( storable ) ;
      element.DeleteEntity( schema ) ;

      if ( element is DataStorage ds && 0 == ds.GetEntitySchemaGuids().Count ) {
        element.Document.Delete( element.Id ) ;
      }
    }

    private static Entity CreateEntity( Element element, StorableBase storable )
    {
      var schema = MakeCertainSchema( storable ) ;
      var entity = new Entity( schema ) ;
      storable.SaveAllFields( new FieldWriter( element, entity ) ) ;

      return entity ;
    }

    private static TStorableBase CreateFromEntity<TStorableBase>( Entity entity, Element ownerElement ) where TStorableBase : StorableBase
    {
      var storable = StorableBase.CreateFromEntity<TStorableBase>( ownerElement ) ;
      storable.LoadAllFields( new FieldReader( ownerElement, entity ) ) ;
      return storable ;
    }

    private static Schema MakeCertainSchema( StorableBase storable )
    {
      return Schema.Lookup( storable.GetType().GUID ) ?? CreateSchema( storable ) ;
    }

    private static Schema CreateSchema( StorableBase storable )
    {
      var generator = new FieldGenerator( storable.GetType() ) ;
      storable.SetupAllFields( generator ) ;
      return generator.CreateSchema() ;
    }
  }
}