using System ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public static class CommandParameterStorage
  {
    private static object? _value = null ;
    
    public static void Set<TParam>( TParam parameters )
    {
      _value = parameters ;
    }

    public static TParam? Pop<TParam>()
    {
      var value = _value ;
      _value = null ;

      return ( value is TParam param ) ? param : default ;
    }
  }
}