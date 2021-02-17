using System ;
using System.Collections.Generic ;
using System.Runtime.CompilerServices ;

namespace Arent3d.Revit
{
  public class ProgressEventArgs : EventArgs
  {
    public double PreviousValue { get ; }
    public double CurrentValue { get ; }
    public bool IsFinished { get ; }

    public ProgressEventArgs( double previousValue, double currentValue, bool isFinished )
    {
      PreviousValue = previousValue ;
      CurrentValue = currentValue ;
      IsFinished = isFinished ;
    }
  }

  public interface IProgressData : IDisposable
  {
    event EventHandler<ProgressEventArgs>? Progress ;

    double Position { get ; }

    IProgressData Reserve( double value, [CallerFilePath] string path = "", [CallerLineNumber] int lineNum = 0 ) ;
    void Finish() ;
    void Step( double value ) ;
  }

  public class ProgressData : IProgressData
  {
    public event EventHandler<ProgressEventArgs>? Progress ;
    private ProgressData? _child = null ;
    private readonly string? _callerFilePath ;
    private readonly int _callerLineNum ;
    private bool _isFinished = false ;

    public double Position { get ; private set ; }

    public ProgressData() : this( null, 0 )
    {
    }

    private ProgressData( string? callerFilePath, int callerLineNum )
    {
      _callerFilePath = callerFilePath ;
      _callerLineNum = callerLineNum ;
      Position = 0 ;
    }

    ~ProgressData()
    {
      if ( null != _callerFilePath ) {
        throw new InvalidOperationException( $"{nameof( ProgressData )} must be manually disposed!\n  Constructed by {_callerFilePath}, l.{_callerLineNum}" ) ;
      }
    }

    void IDisposable.Dispose()
    {
      Finish() ;
    }

    public IProgressData Reserve( double value, [CallerFilePath] string path = "", [CallerLineNumber] int lineNum = 0 )
    {
      if ( path == null ) throw new ArgumentNullException( nameof( path ) ) ;
      if ( value < 0 ) throw new ArgumentOutOfRangeException() ;
      if ( null != _child ) throw new InvalidOperationException() ;

      var orgPos = Position ;
      var newPos = Math.Min( 1, Position + value ) ;

      var data = new ProgressData( path, lineNum ) ;
      data.Progress += ( _, e ) =>
      {
        if ( e.IsFinished ) {
          _child = null ;
        }
        var prevPos = Position ;
        Position =  ( 1 - e.CurrentValue ) * orgPos + e.CurrentValue * newPos  ;
        OnProgress( prevPos, Position, false ) ;
      } ;
      _child = data ;

      return data ;
    }

    public void Finish()
    {
      if ( _isFinished ) return ;

      GC.SuppressFinalize( this ) ;
      _isFinished = true ;

      if ( null != _child ) {
        _child.Finish() ;
      }
      else {
        var prevPos = Position ;
        Position = 1 ;
        OnProgress( prevPos, Position, true ) ;
      }
    }

    public void Step( double value )
    {
      if ( value < 0 ) throw new ArgumentOutOfRangeException() ;

      var prevPos = Position ;
      Position = Math.Min( 1, Position + value ) ;
      OnProgress( prevPos, Position, false ) ;
    }

    private void OnProgress( double prevPos, double nextPos, bool isFinished )
    {
      Progress?.Invoke( this, new ProgressEventArgs( prevPos, nextPos, isFinished ) ) ;
    }
  }

  public static class ProgressDataExtensions
  {
    /// <summary>
    /// Enumerate and progress a collection action.
    /// </summary>
    /// <remarks>
    /// It is not equivalent to
    /// <code>
    /// var n = col.Count() ;
    /// foreach ( var item in col ) {
    ///   using ( progressData?.Reserve( 1.0 / n ) ) {
    ///     func( item ) ;
    ///   }
    /// }
    /// </code>
    /// because this method includes collection enumeration into the using block.
    /// </remarks>
    /// <param name="progressData">Progress data.</param>
    /// <param name="n">Assumed enumeration count.</param>
    /// <param name="col">A collection.</param>
    /// <param name="func">An operation which is applied for each elements in <see cref="col"/>.</param>
    /// <typeparam name="T">Item type of the collection.</typeparam>
    public static void ForEach<T>( this IProgressData? progressData, int n, IEnumerable<T> col, Action<T> func )
    {
      var step = 1.0 / n ;
      using var enumerator = col.GetEnumerator() ;
      while ( true ) {
        using ( progressData?.Reserve( step ) ) {
          if ( false == enumerator.MoveNext() ) {
            return ;
          }

          func( enumerator.Current ) ;
        }
      }
    }

    /// <summary>
    /// Enumerate and progress a collection action.
    /// </summary>
    /// <remarks>
    /// It is not equivalent to
    /// <code>
    /// foreach ( var item in col ) {
    ///   using ( progressData?.Reserve( 1.0 / col.Count ) ) {
    ///     func( item ) ;
    ///   }
    /// }
    /// </code>
    /// because this method includes collection enumeration into the using block.
    /// </remarks>
    /// <param name="progressData">Progress data.</param>
    /// <param name="col">A collection.</param>
    /// <param name="func">An operation which is applied for each elements in <see cref="col"/>.</param>
    /// <typeparam name="T">Item type of the collection.</typeparam>
    public static void ForEach<T>( this IProgressData? progressData, IReadOnlyCollection<T> col, Action<T> func )
    {
      progressData.ForEach( col.Count, col, func ) ;
    }
  }
}