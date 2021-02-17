using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Runtime.CompilerServices ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public class TempColor : ICollection<ElementId>, IDisposable
  {
    private readonly Color _color ;
    private readonly string? _callerFilePath ;
    private readonly int _callerLineNum ;
    private readonly View _view ;
    private Dictionary<ElementId, OverrideGraphicSettings>? _targets = new() ;

    public int Count => Targets.Count ;
    public bool IsReadOnly => false ;

    public TempColor( View view, Color color, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNum = 0 )
    {
      _view = view ;
      _color = color ;
      _callerFilePath = callerFilePath ;
      _callerLineNum = callerLineNum ;
    }
    
    ~TempColor()
    {
      if ( null != _callerFilePath ) {
        throw new InvalidOperationException( $"{nameof( ProgressData )} must be manually disposed!\n  Constructed by {_callerFilePath}, l.{_callerLineNum}" ) ;
      }
    }

    public void Dispose()
    {
      if ( null == _targets ) return ;

      GC.SuppressFinalize( this ) ;
      Clear() ;
      _targets = null ;
    }

    private void RevertColors( IEnumerable<KeyValuePair<ElementId, OverrideGraphicSettings>> targets )
    {
      foreach ( var (elmId, ogs) in targets ) {
        _view.SetElementOverrides( elmId, ogs ) ;
      }
    }

    private Dictionary<ElementId, OverrideGraphicSettings> Targets => _targets ?? throw new ObjectDisposedException( nameof( TempColor ) ) ;

    public void Add( ElementId item )
    {
      if ( Targets.TryGetValue( item, out _ ) ) return ;

      _targets!.Add( item, _view.GetElementOverrides( item ) ) ;
      _view.SetOverriddenColor( item, _color ) ;
    }

    public void AddRange( IEnumerable<ElementId> items )
    {
      items.ForEach( Add ) ;
    }

    public void Clear()
    {
      RevertColors( Targets ) ;
      _targets!.Clear() ;
    }

    public bool Contains( ElementId item ) => Targets.ContainsKey( item ) ;

    public void CopyTo( ElementId[] array, int arrayIndex ) => Targets.Keys.CopyTo( array, arrayIndex ) ;

    public bool Remove( ElementId item )
    {
      if ( false == Targets.TryGetValue( item, out var ogs ) ) return false ;
      RevertColors( new[] { new KeyValuePair<ElementId, OverrideGraphicSettings>( item, ogs ) } ) ;

      _targets!.Remove( item ) ;
      return true ;
    }

    public IEnumerator<ElementId> GetEnumerator() => Targets.Keys.GetEnumerator() ;
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;
  }
}