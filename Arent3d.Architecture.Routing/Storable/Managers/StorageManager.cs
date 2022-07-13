using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Reflection ;
using System.Runtime.InteropServices ;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable.Managers
{
  public static class StorageManager
  {
    public static void SetData<T>( this Element element, T? data ) where T : class
    {
      var schema = FindOrCreateSchema<T>() ;
      var entity = element.GetEntity( schema ) ;
      if ( entity.IsValid() )
        element.DeleteEntity( schema ) ;
      
      if(null == data)
        return;

      entity = new Entity( schema ) ;
      var propertyInfos = GetInfoProperty( typeof( T ) ) ;
      foreach ( var propertyInfo in propertyInfos ) {
        var value = propertyInfo.GetValue( data ) ;
        if(null == value)
          continue;

        var field = schema.GetField( propertyInfo.Name ) ;
        if(null == field)
          continue;

        var method = typeof( Entity ).GetMethods( )
          .SingleOrDefault(x => x.Name == nameof(Entity.Set) && x.GetParameters().Length == 2 && x.GetParameters()[0].ParameterType == typeof(Field)) ;
        if(null == method)
          continue;
        
        var genericMethod = method.MakeGenericMethod( propertyInfo.PropertyType ) ;
        genericMethod.Invoke( entity, new[] { field, value } ) ;
      }

      element.SetEntity( entity ) ;
    }

    public static T? GetData<T>( this Element element ) where T : class, new()
    {
      var schema = FindSchema<T>() ;
      if ( null == schema )
        return null ;

      var entity = element.GetEntity( schema ) ;
      if ( ! entity.IsValid() )
        return null ;

      var instance = new T() ;
      var infoProperties = GetInfoProperty<T>() ;
      foreach ( var (propertyName, propertyType) in infoProperties ) {
        var field = schema.GetField( propertyName ) ;
        if(null == field)
          continue;

        var method = typeof( Entity ).GetMethods( )
          .SingleOrDefault(x => x.Name == nameof(Entity.Get) && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(Field)) ;
        if(null == method)
          continue;
        
        var genericMethod = method.MakeGenericMethod( propertyType ) ;
        var value = genericMethod.Invoke( entity, new object[] { field } ) ;
        if(null == value)
          continue;

        var propertyInfo = instance.GetType().GetProperty( propertyName ) ;
        if(null == propertyInfo)
          continue;
        
        propertyInfo.SetValue(instance, value);
      }

      return instance ;
    }

    private static Guid GetValueGuidAttribute<T>( ) where T : class
    {
      var attribute = typeof(T).GetCustomAttribute<GuidAttribute>( false ) ;
      if ( attribute is null )
        throw new Exception( $"Type {nameof( T )} must have {nameof(GuidAttribute)}." ) ;

      return Guid.TryParse( attribute.Value, out var value) ? value : throw new Exception( "GUID value is not in the correct format." ) ;
    }
    
    private static Schema FindOrCreateSchema<T>() where T : class
    {
      var schema = FindSchema<T>() ;
      return schema ?? CreateSchema<T>() ;
    }

    private static Schema? FindSchema<T>() where T : class
    {
      var schemaId = GetValueGuidAttribute<T>() ;
      return Schema.Lookup( schemaId ) ;
    }

    private static Schema CreateSchema<T>() where T : class
    {
      var schema = FindSchema<T>() ;
      if ( null != schema )
        throw new Exception( $"The schema {schema.SchemaName} already exists!" ) ;

      var schemaId = GetValueGuidAttribute<T>() ;
      var schemaBuilder = new SchemaBuilder( schemaId ) ;
      schemaBuilder.SetReadAccessLevel( AccessLevel.Public ) ;
      schemaBuilder.SetWriteAccessLevel( AccessLevel.Vendor ) ;
      schemaBuilder.SetVendorId( "com.arent3d" ) ;
      schemaBuilder.SetSchemaName( nameof( T ) ) ;

      var infoProperties = GetInfoProperty<T>() ;
      foreach ( var (propertyName, propertyType) in infoProperties ) {
        var fieldBuilder = schemaBuilder.AddSimpleField( propertyName, propertyType ) ;
        fieldBuilder.SetDocumentation( $"The schema field is {propertyName}." ) ;
      }

      return schemaBuilder.Finish() ;
    }

    private static IEnumerable<(string PropertyName, Type PropertyType)> GetInfoProperty<T>() where T : class
    {
      var propertyInfos = GetInfoProperty( typeof( T ) ) ;
      return propertyInfos.Select( x => ( x.Name, x.PropertyType ) ) ;
    }
    
    private static IList<PropertyInfo> GetInfoProperty(Type type)
    {
      var propertyInfos = type.GetProperties( BindingFlags.Public | BindingFlags.Instance) ;
      if ( ! propertyInfos.Any() )
        throw new Exception( $"Not found the public property in the type {nameof( type )}." ) ;
      
      if ( propertyInfos.Any( x => ! IsAccepType( x.PropertyType ) ) )
        throw new Exception( $"The accepted data type of the property is {string.Join( ", ", TypeAccepts.Select( x => x.Name ) )}." ) ;

      return propertyInfos ;
    }

    private static bool IsAccepType( Type type )
    {
      return TypeAccepts.Any( x => x == type ) ;
    }

    private static HashSet<Type> TypeAccepts => new( new List<Type>
    {
      typeof(int),
      typeof(short),
      typeof(byte),
      typeof(double),
      typeof(float),
      typeof(bool),
      typeof(string),
      typeof(Guid),
      typeof(ElementId),
      typeof(XYZ),
      typeof(UV),
      typeof(Entity)
    } ) ;
  }
}