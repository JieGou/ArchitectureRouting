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
    public static bool HasStorable<TStorableBase>( this Document document, ElementId elmId ) where TStorableBase : StorableBase
    {
      if ( document.GetElement( elmId ) is not { } element ) return false ;
      return element.HasStorable<TStorableBase>() ;
    }

    public static bool HasStorable<TStorableBase>( this Element element ) where TStorableBase : StorableBase
    {
      if ( Schema.Lookup( typeof( TStorableBase ).GUID ) is not { } schema ) return false ;

      var entity = element.GetEntity( schema ) ;
      if ( null == entity || false == entity.IsValidObject || false == entity.IsValid() ) return false ;

      return true ;
    }

    public static IEnumerable<ElementId> FilterStorableElements<TStorableBase>( this Document document, IEnumerable<ElementId> elmIds ) where TStorableBase : StorableBase
    {
      if ( Schema.Lookup( typeof( TStorableBase ).GUID ) is not { } schema ) return Enumerable.Empty<ElementId>() ;

      return elmIds.Where( elmId =>
      {
        if ( document.GetElement( elmId ) is not { } element ) return false ;

        var entity = element.GetEntity( schema ) ;
        if ( null == entity || false == entity.IsValidObject || false == entity.IsValid() ) return false ;

        return true ;
      } ) ;
    }

    public static IEnumerable<TStorableBase> GetAllStorables<TStorableBase>( this Document document ) where TStorableBase : StorableBase
    {
      if ( Schema.Lookup( typeof( TStorableBase ).GUID ) is not { } schema ) yield break ;

      foreach ( var element in document.GetAllElements<DataStorage>().Where( new ExtensibleStorageFilter( typeof( TStorableBase ).GUID ) ) ) {
        var entity = element.GetEntity( schema ) ;
        if ( null == entity || false == entity.IsValidObject || false == entity.IsValid() ) continue ;

        yield return CreateFromEntity<TStorableBase>( entity, element ) ;
      }
    }
    public static TStorableBase? GetStorable<TStorableBase>( this DataStorage element ) where TStorableBase : StorableBase
    {
      if ( Schema.Lookup( typeof( TStorableBase ).GUID ) is not { } schema ) return null ;

      var entity = element.GetEntity( schema ) ;
      if ( null == entity || false == entity.IsValidObject || false == entity.IsValid() ) return null ;

      return CreateFromEntity<TStorableBase>( entity, element ) ;
    }

    public static bool HasAnyDerivedStorable<TStorableBase>( this Document document, ElementId elmId ) where TStorableBase : StorableBase
    {
      if ( document.GetElement( elmId ) is not { } element ) return false ;

      return element.HasAnyDerivedStorable<TStorableBase>() ;
    }

    public static bool HasAnyDerivedStorable<TStorableBase>( this Element element ) where TStorableBase : StorableBase
    {
      foreach ( var type in GetAllDerivedClasses<TStorableBase>() ) {
        if ( Schema.Lookup( type.GUID ) is not { } schema ) continue ;
        var entity = element.GetEntity( schema ) ;
        if ( null != entity && entity.IsValidObject && entity.IsValid() ) return true ;
      }

      return false ;
    }

    public static IEnumerable<ElementId> FilterDerivedStorableElements<TStorableBase>( this Document document, IEnumerable<ElementId> elmIds ) where TStorableBase : StorableBase
    {
      return elmIds.Where( document.HasAnyDerivedStorable<TStorableBase> ) ;
    }

    public static IEnumerable<TStorableBase> GetAllDerivedStorables<TStorableBase>( this Document document ) where TStorableBase : StorableBase
    {
      ElementFilter? totalFilter = null ;
      var dic = new Dictionary<Guid, Type>() ;
      foreach ( var type in GetAllDerivedClasses<TStorableBase>() ) {
        dic.Add( type.GUID, type ) ;
        var filter = new ExtensibleStorageFilter( type.GUID ) ;
        if ( null == totalFilter ) {
          totalFilter = filter ;
        }
        else {
          totalFilter = new LogicalOrFilter( totalFilter, filter ) ;
        }
      }
      if ( null == totalFilter ) yield break ;

      foreach ( var element in document.GetAllElements<DataStorage>().Where( totalFilter ) ) {
        foreach ( var guid in element.GetEntitySchemaGuids() ) {
          if ( false == dic.TryGetValue( guid, out var type ) ) continue ;

          var entity = element.GetEntity( Schema.Lookup( guid ) ) ;
          if ( null == entity || false == entity.IsValidObject || false == entity.IsValid() ) continue ;

          if ( CreateFromEntity( type, entity, element ) is not TStorableBase storable ) continue ;
          yield return storable ;
        }
      }
    }
    public static TStorableBase? GetDerivedStorable<TStorableBase>( this DataStorage element ) where TStorableBase : StorableBase
    {
      foreach ( var type in GetAllDerivedClasses<TStorableBase>() ) {
        if ( Schema.Lookup( type.GUID ) is not { } schema ) continue ;

        var entity = element.GetEntity( schema ) ;
        if ( null == entity || false == entity.IsValidObject || false == entity.IsValid() ) continue ;

        if ( CreateFromEntity( type, entity, element ) is TStorableBase storable ) return storable ;
      }

      return null ;
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

    private static TStorableBase CreateFromEntity<TStorableBase>( Entity entity, DataStorage ownerElement ) where TStorableBase : StorableBase
    {
      var storable = StorableBase.CreateFromEntity<TStorableBase>( ownerElement ) ;
      storable.LoadAllFields( new FieldReader( ownerElement, entity ) ) ;
      return storable ;
    }

    private static StorableBase CreateFromEntity( Type storableType, Entity entity, DataStorage ownerElement )
    {
      var storable = StorableBase.CreateFromEntity( storableType, ownerElement ) ;
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


    #region Reflections

    private static readonly Dictionary<Type, IReadOnlyCollection<Type>> _allDerivedClasses = new() ;
    private static readonly HashSet<Assembly> _registeredAssemblies = new() ;
    
    private static IReadOnlyCollection<Type> GetAllDerivedClasses<TStorableBase>() where TStorableBase : StorableBase
    {
      var type = typeof( TStorableBase ) ;
      if ( false == _allDerivedClasses.TryGetValue( type, out var list ) ) {
        list = _registeredAssemblies.SelectMany( assembly => GetDerivedClassesOf( type, assembly ) ).EnumerateAll() ;
        _allDerivedClasses.Add( type, list ) ;
      }
      return list ;
    }

    private static IEnumerable<Type> GetDerivedClassesOf( Type type, Assembly assembly )
    {
      return assembly.GetTypes().Where( t => false == t.IsAbstract && ( t == type || t.IsSubclassOf( type ) ) ) ;
    }

    public static void RegisterAssembly( Assembly assembly )
    {
      if ( _registeredAssemblies.Add( assembly ) ) {
        // reset caches
        _allDerivedClasses.Clear() ;
      }
    }

    #endregion
  }
}