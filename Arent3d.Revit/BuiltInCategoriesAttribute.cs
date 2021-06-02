using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Reflection ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  [Flags]
  public enum ExternalParameterType
  {
    Instance = 0x1,
    Type = 0x2,
    Both = Instance | Type,
  }

  [AttributeUsage( AttributeTargets.Field, AllowMultiple = true )]
  public class BuiltInCategoriesAttribute : Attribute
  {
    private readonly ExternalParameterType _parameterType ;
    public BuiltInParameterGroup ParameterGroup { get ; }
    private readonly BuiltInCategory[] _categories ;

    public BuiltInCategoriesAttribute( ExternalParameterType parameterType, BuiltInParameterGroup parameterGroup, params BuiltInCategory[] categories )
    {
      _parameterType = parameterType ;
      ParameterGroup = parameterGroup ;
      _categories = categories ;
    }

    public BuiltInCategoriesAttribute( ExternalParameterType parameterType, BuiltInParameterGroup parameterGroup, Type typeWhichCategoriesFieldIsDefined, string categoriesFieldName )
    {
      _parameterType = parameterType ;
      ParameterGroup = parameterGroup ;
      _categories = GetCategories( typeWhichCategoriesFieldIsDefined, categoriesFieldName ) ;
    }

    public IEnumerable<Binding> GetBindings( Document document )
    {
      if ( 0 == _categories.Length ) yield break ;

      var create = document.Application.Create ;
      var categorySet = create.NewCategorySet() ;
      foreach ( var cat in _categories ) {
        if ( document.Settings.Categories.get_Item( cat ) is not { } category ) continue ;

        categorySet.Insert( category ) ;
      }

      if ( 0 != ( _parameterType & ExternalParameterType.Instance ) ) {
        yield return create.NewInstanceBinding( categorySet ) ;
      }
      if ( 0 != ( _parameterType & ExternalParameterType.Type ) ) {
        yield return create.NewTypeBinding( categorySet ) ;
      }
    }



    private static readonly Dictionary<(Type, string), BuiltInCategory[]> _categoriesCache = new() ;

    private static BuiltInCategory[] GetCategories( Type typeWhichCategoriesFieldIsDefined, string categoriesFieldName )
    {
      var tuple = ( typeWhichCategoriesFieldIsDefined, categoriesFieldName ) ;
      if ( false == _categoriesCache.TryGetValue( tuple, out var array ) ) {
        array = GetCategoriesImpl( typeWhichCategoriesFieldIsDefined, categoriesFieldName ).ToArray() ;
        _categoriesCache.Add( tuple, array ) ;
      }

      return array ;
    }

    private static IEnumerable<BuiltInCategory> GetCategoriesImpl( Type typeWhichCategoriesFieldIsDefined, string categoriesFieldName )
    {
      if ( typeWhichCategoriesFieldIsDefined.GetField( categoriesFieldName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic ) is { } field ) {
        if ( field.FieldType.HasInterface<IEnumerable<BuiltInCategory>>() && field.GetValue( null ) is IEnumerable<BuiltInCategory> categories ) {
          return categories ;
        }
      }

      if ( typeWhichCategoriesFieldIsDefined.GetProperty( categoriesFieldName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic ) is { } property ) {
        if ( property.CanRead && property.PropertyType.HasInterface<IEnumerable<BuiltInCategory>>() && property.GetValue( null ) is IEnumerable<BuiltInCategory> categories ) {
          return categories ;
        }
      }

      if ( typeWhichCategoriesFieldIsDefined.GetMethod( categoriesFieldName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null ) is { } method ) {
        if ( false == method.IsAbstract && method.ReturnType.HasInterface<IEnumerable<BuiltInCategory>>() && method.Invoke( null, Array.Empty<object>() ) is IEnumerable<BuiltInCategory> categories ) {
          return categories ;
        }
      }

      return Enumerable.Empty<BuiltInCategory>() ;
    }
  }
}