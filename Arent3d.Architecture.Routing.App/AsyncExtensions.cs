using System.Collections.Generic ;
using System.Threading ;
using System.Threading.Tasks ;

namespace Arent3d.Architecture.Routing.App
{
  public static class AsyncExtensions
  {
    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>( this IEnumerable<T> col )
    {
      return new AsyncEnumerable<T>( col ) ;
    }

    private class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
      private readonly IEnumerable<T> _col ;
      public AsyncEnumerable( IEnumerable<T> col )
      {
        _col = col ;
      }

      public IAsyncEnumerator<T> GetAsyncEnumerator( CancellationToken cancellationToken = new CancellationToken() )
      {
        return new AsyncEnumerator( _col.GetEnumerator() ) ;
      }

      private class AsyncEnumerator : IAsyncEnumerator<T>
      {
        private readonly IEnumerator<T> _enu ;

        public AsyncEnumerator( IEnumerator<T> enu )
        {
          _enu = enu ;
        }

        public async ValueTask DisposeAsync()
        {
          await Task.Yield() ;
          _enu.Dispose() ;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
          await Task.Yield() ;
          return _enu.MoveNext() ;
        }

        public T Current => _enu.Current ;
      }
    }
  }
}