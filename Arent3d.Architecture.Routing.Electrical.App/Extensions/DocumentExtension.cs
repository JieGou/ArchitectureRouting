using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Extensions
{
  public static class DocumentExtension
  {
    public static IEnumerable<T> GetAllTypes<T>( this Document document, Func<T, bool> func) where T : ElementType
    {
      var collector = new FilteredElementCollector( document ) ;
      return collector.OfClass( typeof( T ) ).WhereElementIsElementType().OfType<T>().Where( func ) ;
    }

    public static IEnumerable<T> GetAllInstances<T>( this Document document) where T : Element
    {
      var collector = new FilteredElementCollector( document ) ;
      return collector.OfClass( typeof( T ) ).WhereElementIsNotElementType().OfType<T>();
    }
  }
}