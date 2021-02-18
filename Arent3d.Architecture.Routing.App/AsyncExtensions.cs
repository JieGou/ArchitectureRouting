using System.Collections.Generic ;
using System.Threading.Tasks ;

namespace Arent3d.Architecture.Routing.App
{
  public static class AsyncExtensions
  {
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>( this IEnumerable<T> col )
    {
      await Task.Yield() ;

      foreach ( var item in col ) {
        yield return item ;
      }
    }
  }
}