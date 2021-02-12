using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;

namespace Arent3d.Revit
{
  /// <summary>
  /// Defines extension methods for Revit data.
  /// </summary>
  public static class RevitExtensions
  {
    public static BuiltInCategory GetBuiltInCategory( this Element elm )
    {
      var category = elm.Category ;
      if ( null == category ) return BuiltInCategory.INVALID ;
      return category.GetBuiltInCategory() ;
    }
    public static BuiltInCategory GetBuiltInCategory( this Category category )
    {
      return (BuiltInCategory) category.Id.IntegerValue ;
    }
    
    public static IElementEnumerable<TElement> GetAllElements<TElement>( this Document document ) where TElement : Element
    {
      return new FilteredElementCollectorBuilder<TElement>( document ) ;
    }
    public static IElementEnumerable<TElement> GetAllElements<TElement>( this Document document, Type type ) where TElement : Element
    {
      if ( false == typeof( TElement ).IsAssignableFrom( type ) ) throw new ArgumentException() ;

      return new FilteredElementCollectorBuilder<TElement>( document, type ) ;
    }
    public static IElementEnumerable<FamilyInstance> GetAllFamilyInstances( this Document document, FamilySymbol familySymbol )
    {
      return new FilteredElementCollectorBuilder<FamilyInstance>( document ).Where( new FamilyInstanceFilter( document, familySymbol.Id ) ) ;
    }

    public static TElement? GetElementById<TElement>( this Document document, ElementId elementId ) where TElement : Element
    {
      return document.GetElement( elementId ) as TElement ;
    }
    public static TElement? GetElementById<TElement>( this Document document, int elementId ) where TElement : Element
    {
      return document.GetElementById<TElement>( new ElementId( elementId ) ) ;
    }
  }
}