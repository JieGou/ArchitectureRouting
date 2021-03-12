using System ;
using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing
{
  internal class SizeTable<TValue> where TValue : struct
  {
    private readonly Func<TValue, double> _generator ;
    private readonly Dictionary<TValue, double> _dic = new() ;

    public SizeTable( Func<TValue, double> generator )
    {
      _generator = generator ;
    }

    public double Get( TValue value )
    {
      if ( false == _dic.TryGetValue( value, out var result ) ) {
        result = _generator( value ) ;
        _dic.Add( value, result ) ;
      }

      return result ;
    }
  }
}