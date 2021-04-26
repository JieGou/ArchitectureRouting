using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public class ElementEqualityComparer<TElement> : IEqualityComparer<TElement> where TElement : Element
  {
    public static IEqualityComparer<TElement> Default { get ; } = new ElementEqualityComparer<TElement>() ;

    private ElementEqualityComparer()
    {
    }

    public bool Equals( TElement x, TElement y )
    {
      return x.GetValidId() == y.GetValidId() ;
    }

    public int GetHashCode( TElement obj ) => obj.GetValidId().IntegerValue.GetHashCode() ;
  }
}