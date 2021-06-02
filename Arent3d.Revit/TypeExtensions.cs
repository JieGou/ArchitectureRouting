using System ;

namespace Arent3d.Revit
{
  public static class TypeExtensions
  {
    public static bool HasInterface<TInterface>( this Type type )
    {
      return ( 0 != type.FindInterfaces( ( t, _ ) => t == typeof( TInterface ), null ).Length ) ;
    }
  }
}