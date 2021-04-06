using System ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App
{
  public static class EnumerableExtensions
  {
    public static int FindDoubleIndex( this IEnumerable<double> source, double? value, Document doc )
    {
      if ( value != null ) {
        var doubles = source.TakeWhile( d => Math.Abs( ( d - (double) value ) ) > doc.Application.VertexTolerance ) ;
        var targetIndex = doubles.Count() ;

        return targetIndex ;
      }

      return -1 ;
    }
  }
}