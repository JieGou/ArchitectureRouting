using System ;
using System.Linq ;
using Autodesk.Revit.UI ;

namespace Arent3d.Revit
{
  public static class RevitExtensions
  {
    private static bool HasInterface<T>( this Type type ) => type.HasInterface( typeof( T ) ) ;

    private static bool HasInterface( this Type type, Type interfaceType )
    {
      if ( false == interfaceType.IsInterface ) throw new ArgumentException( nameof( interfaceType ) ) ;

      return type.FindInterfaces( ( t, _ ) => t == interfaceType, null ).Any() ;
    }

    public static bool IsExternalCommand( this Type type )
    {
      return type.HasInterface<IExternalCommand>() ;
    }
  }
}