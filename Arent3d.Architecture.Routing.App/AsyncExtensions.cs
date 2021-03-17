using System ;
using System.Collections.Generic ;
using System.Threading ;
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

    public static async IAsyncEnumerable<T> Concat<T>( this IAsyncEnumerable<T> col1, IAsyncEnumerable<T> col2 )
    {
      await foreach ( var item in col1 ) {
        yield return item ;
      }

      await foreach ( var item in col2 ) {
        yield return item ;
      }
    }
  }
  public static class AsyncEnumerable
  {
    public static IAsyncEnumerable<T> Empty<T>()
    {
      return EmptyAsync<T>.Instance ;
    }

    private class EmptyAsync<T> : IAsyncEnumerable<T>
    {
      public static EmptyAsync<T> Instance { get ; } = new EmptyAsync<T>() ;

      private EmptyAsync()
      {
      }

      public IAsyncEnumerator<T> GetAsyncEnumerator( CancellationToken cancellationToken = new CancellationToken() )
      {
        return AsyncEnumerator.Instance ;
      }

      private class AsyncEnumerator : IAsyncEnumerator<T>
      {
        public static AsyncEnumerator Instance { get ; } = new AsyncEnumerator() ;

        private AsyncEnumerator()
        {
        }

        public ValueTask DisposeAsync()
        {
          return new ValueTask( Task.CompletedTask ) ;
        }

        public ValueTask<bool> MoveNextAsync()
        {
          return new ValueTask<bool>( false ) ;
        }

        public T Current => default! ;
      }
    }
  }
}