using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.AppBase.Extensions
{
  public static class ListExtensions
  {
    public static void Swap<T>(this List<T> list, int i, int j)
    {
      ( list[i], list[j] ) = ( list[j], list[i] ) ;
    }
  }
}