using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.DB.Plumbing ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Defines extension methods for Revit data.
  /// </summary>
  public static class RevitExtensions
  {
    public static IEnumerable<TElement> GetAllElements<TElement>( this Document document ) where TElement : Element
    {
      return new FilteredElementCollector( document ).OfClass( typeof( TElement ) ).OfType<TElement>() ;
    }
    public static IEnumerable<TElement> GetAllElementsInCategory<TElement>( this Document document, BuiltInCategory category ) where TElement : Element
    {
      return new FilteredElementCollector( document ).OfCategory( category ).OfClass( typeof( TElement ) ).OfType<TElement>() ;
    }
    public static IEnumerable<Level> GetAllLevels( this Document document )
    {
      return new FilteredElementCollector( document ).OfCategory( BuiltInCategory.OST_Levels ).WhereElementIsNotElementType().OfType<Level>() ;
    }

    public static TElement? GetElementById<TElement>( this Document document, int elementId ) where TElement : Element
    {
      return document.GetElement( new ElementId( elementId ) ) as TElement ;
    }
  }
}