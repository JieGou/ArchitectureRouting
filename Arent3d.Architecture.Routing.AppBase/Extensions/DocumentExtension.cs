using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Extensions
{
  public static class DocumentExtension
  {
    public static List<T> GetAllInstances<T>(this Document document) where T : Element
    {
      return new FilteredElementCollector(document).OfClass(typeof(T)).OfType<T>().ToList();
    }
    public static List<T> GetAllInstances<T>(this Document document, View view) where T : Element
    {
      return new FilteredElementCollector(document, view.Id).OfClass(typeof(T)).OfType<T>().ToList();
    }
    public static List<T> GetAllTypes<T>(this Document document) where T : ElementType
    {
      return new FilteredElementCollector(document).OfClass(typeof(T)).WhereElementIsElementType().OfType<T>().ToList();
    }
    public static List<T> GetAllTypes<T>(this Document document, Func<T, bool> func ) where T : ElementType
    {
      return new FilteredElementCollector( document ).OfClass( typeof( T ) ).WhereElementIsElementType().OfType<T>().Where(func).ToList();
    }
  }
}