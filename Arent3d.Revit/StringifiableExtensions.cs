using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public static class StringifiableExtensions
  {
    public static void Add( this Stringifier stringifier, ElementId elementId )
    {
      stringifier.Add( elementId.IntegerValue ) ;
    }
    public static void Add( this Stringifier stringifier, Element? element )
    {
      stringifier.Add( element.GetValidId() ) ;
    }

    public static ElementId? GetElementId( this Parser parser, int index )
    {
      if ( parser.GetInt( index ) is not { } elementId ) return null ;
      if ( elementId == ElementId.InvalidElementId.IntegerValue ) return ElementId.InvalidElementId ;

      return new ElementId( elementId ) ;
    }
    public static TElement? GetElement<TElement>( this Parser parser, int index, Document document ) where TElement : Element
    {
      if ( parser.GetElementId( index ) is not { } elementId ) return null ;

      return document.GetElementById<TElement>( elementId ) ;
    }
  }
}