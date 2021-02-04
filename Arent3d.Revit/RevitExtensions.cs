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
    public static IEnumerable<TElement> GetAllElements<TElement>( this Document document ) where TElement : Element
    {
      return new FilteredElementCollector( document ).OfClass( typeof( TElement ) ).OfType<TElement>() ;
    }
    public static IEnumerable<FamilyInstance> GetAllFamilyInstances( this Document document, FamilySymbol familySymbol )
    {
      return new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) ).WherePasses( new FamilyInstanceFilter( document, familySymbol.Id ) ).OfType<FamilyInstance>() ;
    }
    public static FamilySymbol? GetFamilySymbol( this Document document, BuiltInCategory category, string familyName )
    {
      return new FilteredElementCollector( document ).OfClass( typeof( FamilySymbol ) ).OfCategory( category ).OfType<FamilySymbol>().FirstOrDefault( e => e.FamilyName == familyName ) ;
    }
    public static FamilySymbol? GetFamilySymbol( this Document document, string familyName )
    {
      return new FilteredElementCollector( document ).OfClass( typeof( FamilySymbol ) ).OfType<FamilySymbol>().FirstOrDefault( e => e.FamilyName == familyName ) ;
    }
    public static IEnumerable<TElement> GetAllElementsInCategory<TElement>( this Document document, BuiltInCategory category ) where TElement : Element
    {
      return new FilteredElementCollector( document ).OfCategory( category ).OfClass( typeof( TElement ) ).OfType<TElement>() ;
    }
    public static IEnumerable<Level> GetAllLevels( this Document document )
    {
      return new FilteredElementCollector( document ).OfCategory( BuiltInCategory.OST_Levels ).WhereElementIsNotElementType().OfType<Level>() ;
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