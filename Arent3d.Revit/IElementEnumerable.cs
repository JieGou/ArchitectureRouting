using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public interface IElementEnumerable<out TElement> : IEnumerable<TElement> where TElement : Element
  {
    IElementEnumerable<TElement> Where( ElementFilter filter ) ;
    IElementEnumerable<TElement> OfCategory( BuiltInCategory category ) ;
    IElementEnumerable<TElement> OfCategory( params BuiltInCategory[] categories ) ;
    IElementEnumerable<TElement> OfElementType() ;
    IElementEnumerable<TElement> OfNotElementType() ;
  }

  internal class FilteredElementCollectorBuilder<TElement> : IElementEnumerable<TElement> where TElement : Element
  {
    private readonly Type _type ;
    private readonly Document _document ;
    private List<ElementFilter>? _quickFilters = null ;
    private List<ElementFilter>? _slowFilters = null ;
    private BuiltInCategory[]? _categories = null ;
    private bool? _elementType = null ;

    public FilteredElementCollectorBuilder( Document document )
    {
      _document = document ;
      _type = typeof( TElement ) ;
    }

    public FilteredElementCollectorBuilder( Document document, Type type )
    {
      _document = document ;
      _type = type ;
    }

    public IElementEnumerable<TElement> Where( ElementFilter filter )
    {
      if ( filter is ElementQuickFilter ) {
        _quickFilters ??= new List<ElementFilter>() ;
        _quickFilters.Add( filter ) ;
      }
      else {
        _slowFilters ??= new List<ElementFilter>() ;
        _slowFilters.Add( filter ) ;
      }

      return this ;
    }

    public IElementEnumerable<TElement> OfCategory( BuiltInCategory category )
    {
      if ( null != _categories ) throw new InvalidOperationException() ;

      _categories = new[] { category } ;
      return this ;
    }

    public IElementEnumerable<TElement> OfCategory( params BuiltInCategory[] categories )
    {
      if ( null != _categories ) throw new InvalidOperationException() ;

      if ( categories.Length == 0 ) return this ;

      _categories = categories ;
      return this ;
    }

    public IElementEnumerable<TElement> OfElementType()
    {
      if ( false == _elementType ) throw new InvalidOperationException() ;

      _elementType = true ;
      return this ;
    }

    public IElementEnumerable<TElement> OfNotElementType()
    {
      if ( true == _elementType ) throw new InvalidOperationException() ;
      
      _elementType = false ;
      return this ;
    }

    public IEnumerator<TElement> GetEnumerator()
    {
      return BuildFilteredElementCollector().OfType<TElement>().GetEnumerator() ;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;

    private FilteredElementCollector BuildFilteredElementCollector()
    {
      var collector = new FilteredElementCollector( _document ) ;

      if ( null != _quickFilters ) {
        foreach ( var filter in _quickFilters ) {
          collector.WherePasses( filter ) ;
        }
      }

      if ( null != _categories ) {
        if ( 1 == _categories.Length ) {
          collector.OfCategory( _categories[ 0 ] ) ;
        }
        else {
          collector.WherePasses( new LogicalOrFilter( Array.ConvertAll( _categories, c => (ElementFilter) new ElementCategoryFilter( c ) ) ) ) ;
        }
      }

      if ( _type != typeof( Element ) ) {
        collector.OfClass( _type ) ;
      }

      if ( true == _elementType ) {
        collector.WhereElementIsElementType() ;
      }
      else if ( false == _elementType ) {
        collector.WhereElementIsNotElementType() ;
      }

      if ( null != _slowFilters ) {
        foreach ( var filter in _slowFilters ) {
          collector.WherePasses( filter ) ;
        }
      }

      return collector ;
    }
  }
}