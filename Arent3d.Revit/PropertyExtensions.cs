using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public static class PropertyExtensions
  {
    private static class PropertyDefinitions<TParameterNameEnum> where TParameterNameEnum : Enum
    {
      public static readonly IReadOnlyDictionary<TParameterNameEnum, string> AllParameterNames = NameOnRevitAttribute.ToDictionary<TParameterNameEnum>() ;
      public static readonly IReadOnlyDictionary<TParameterNameEnum, Guid> AllParameterGuids = ParameterGuidAttribute.ToDictionary<TParameterNameEnum>() ;
    }

    public static bool AllParametersAreRegistered<TPropertyEnum>( this Document document ) where TPropertyEnum : Enum
    {
      var currentDefinitions = GetDefinitions( document.ParameterBindings ).Select( d => d.Name ).ToHashSet() ;
      return PropertyDefinitions<TPropertyEnum>.AllParameterNames.Values.All( currentDefinitions.Contains ) ;
    }

    private static IEnumerable<Definition> GetDefinitions( DefinitionBindingMap bindings )
    {
      using var it = bindings.ForwardIterator() ;
      while ( it.MoveNext() ) {
        yield return it.Key ;
      }
    }

    public static void LoadAllAllParametersFromFile( this Document document, IEnumerable<BuiltInCategory> builtInCategorySet, string filePath )
    {
      var app = document.Application ;

      var arentCategorySet = app.Create.NewCategorySet() ;
      foreach ( var cat in builtInCategorySet ) {
        if ( document.Settings.Categories.get_Item( cat ) is not { } category ) continue ;

        arentCategorySet.Insert( category ) ;
      }

      var instanceBinding = app.Create.NewInstanceBinding( arentCategorySet ) ;

      var bindingMap = document.ParameterBindings ;

      foreach ( var definition in SharedParameterReader.GetSharedParameters( document.Application, filePath ) ) {
        if ( bindingMap.Contains( definition ) ) continue ;

        bindingMap.Insert( definition, instanceBinding, BuiltInParameterGroup.PG_IDENTITY_DATA ) ;
      }
    }

    public static void SetProperty<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, double value ) where TPropertyEnum : Enum
    {
      var parameter = elm.GetParameter( propertyEnum ) ?? throw new InvalidOperationException() ;

      if ( StorageType.Double == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }

    public static void SetProperty<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, int value ) where TPropertyEnum : Enum
    {
      var parameter = elm.GetParameter( propertyEnum ) ?? throw new InvalidOperationException() ;

      if ( StorageType.Double == parameter.StorageType ) {
        parameter.Set( (double) value ) ;
      }
      else if ( StorageType.Integer == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }

    public static void SetProperty<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, string value ) where TPropertyEnum : Enum
    {
      var parameter = elm.GetParameter( propertyEnum ) ?? throw new InvalidOperationException() ;

      if ( StorageType.String == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }

    public static void SetProperty<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, ElementId value ) where TPropertyEnum : Enum
    {
      var parameter = elm.GetParameter( propertyEnum ) ?? throw new InvalidOperationException() ;

      if ( StorageType.ElementId == parameter.StorageType ) {
        parameter.Set( value ) ;
      }
      else if ( StorageType.Integer == parameter.StorageType ) {
        parameter.Set( value.IntegerValue ) ;
      }
      else {
        throw new InvalidOperationException() ;
      }
    }
    public static void SetProperty<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, Element? value ) where TPropertyEnum : Enum
    {
      elm.SetProperty( propertyEnum, value.GetValidId() ) ;
    }

    public static int GetPropertyInt<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum ) where TPropertyEnum : Enum
    {
      var parameter = elm.GetParameter( propertyEnum ) ?? throw new InvalidOperationException() ;

      return parameter.StorageType switch
      {
        StorageType.Integer => parameter.AsInteger(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static double GetPropertyDouble<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum ) where TPropertyEnum : Enum
    {
      var parameter = elm.GetParameter( propertyEnum ) ?? throw new InvalidOperationException() ;

      return parameter.StorageType switch
      {
        StorageType.Integer => parameter.AsInteger(),
        StorageType.Double => parameter.AsDouble(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static string GetPropertyString<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum ) where TPropertyEnum : Enum
    {
      var parameter = elm.GetParameter( propertyEnum ) ?? throw new InvalidOperationException() ;

      return parameter.StorageType switch
      {
        StorageType.String => parameter.AsString(),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static ElementId GetPropertyElementId<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum ) where TPropertyEnum : Enum
    {
      var parameter = elm.GetParameter( propertyEnum ) ?? throw new InvalidOperationException() ;

      return parameter.StorageType switch
      {
        StorageType.ElementId => parameter.AsElementId(),
        StorageType.Integer => new ElementId( parameter.AsInteger() ),
        _ => throw new InvalidOperationException(),
      } ;
    }
    public static Element? GetPropertyElement<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum ) where TPropertyEnum : Enum
    {
      return elm.Document.GetElement( elm.GetPropertyElementId( propertyEnum ) ) ;
    }
    public static TElement? GetPropertyElement<TElement, TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum ) where TElement : Element where TPropertyEnum : Enum
    {
      return elm.GetPropertyElement( propertyEnum ) as TElement ;
    }

    public static bool TryGetProperty<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, out int value ) where TPropertyEnum : Enum
    {
      value = default ;

      var parameter = elm.GetParameter( propertyEnum ) ;
      if ( null == parameter ) return false ;

      switch ( parameter.StorageType ) {
        case StorageType.Integer :
          value = parameter.AsInteger() ;
          return true ;

        default :
          return false ;
      }
    }
    public static bool TryGetProperty<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, out double value ) where TPropertyEnum : Enum
    {
      value = default ;

      var parameter = elm.GetParameter( propertyEnum ) ;
      if ( null == parameter ) return false ;

      switch ( parameter.StorageType ) {
        case StorageType.Integer :
          value = parameter.AsInteger() ;
          return true ;

        case StorageType.Double :
          value = parameter.AsDouble() ;
          return true ;

        default :
          return false ;
      }
    }
    public static bool TryGetProperty<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, out string? value ) where TPropertyEnum : Enum
    {
      value = default ;

      var parameter = elm.GetParameter( propertyEnum ) ;
      if ( null == parameter ) return false ;

      switch ( parameter.StorageType ) {
        case StorageType.String :
          value = parameter.AsString() ;
          return true ;

        default :
          return false ;
      }
    }
    public static bool TryGetProperty<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, out ElementId value ) where TPropertyEnum : Enum
    {
      value = ElementId.InvalidElementId ;
      var parameter = elm.GetParameter( propertyEnum ) ;
      if ( null == parameter ) return false ;

      switch ( parameter.StorageType ) {
        case StorageType.ElementId :
          value = parameter.AsElementId() ;
          return true ;

        case StorageType.Integer :
          value = new ElementId( parameter.AsInteger() ) ;
          return true ;

        default :
          return false ;
      }
    }
    public static bool TryGetProperty<TElement, TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, out TElement? value ) where TElement : Element where TPropertyEnum : Enum
    {
      value = default ;
      if ( false == elm.TryGetProperty( propertyEnum, out ElementId elmId ) ) return false ;

      value = elm.Document.GetElementById<TElement>( elmId ) ;
      return true ;
    }

    public static bool HasParameter<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum ) where TPropertyEnum : Enum
    {
      if ( false == PropertyDefinitions<TPropertyEnum>.AllParameterGuids.TryGetValue( propertyEnum, out var guid ) ) return false ;
      return ( true == elm.get_Parameter( guid )?.HasValue ) ;
    }

    public static bool HasParameter<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum, StorageType type ) where TPropertyEnum : Enum
    {
      if ( false == PropertyDefinitions<TPropertyEnum>.AllParameterGuids.TryGetValue( propertyEnum, out var guid ) ) throw new InvalidOperationException() ;
      return ( elm.get_Parameter( guid )?.StorageType == type ) ;
    }

    public static Parameter? GetParameter<TPropertyEnum>( this Element elm, TPropertyEnum propertyEnum ) where TPropertyEnum : Enum
    {
      if ( false == PropertyDefinitions<TPropertyEnum>.AllParameterGuids.TryGetValue( propertyEnum, out var guid ) ) return null ;
      return elm.get_Parameter( guid ) ;
    }

    public static string? GetParameterName<TPropertyEnum>( this Document document, TPropertyEnum propertyEnum ) where TPropertyEnum : Enum
    {
      if ( false == PropertyDefinitions<TPropertyEnum>.AllParameterNames.TryGetValue( propertyEnum, out var name ) ) return null ;
      return name ;
    }
  }
}