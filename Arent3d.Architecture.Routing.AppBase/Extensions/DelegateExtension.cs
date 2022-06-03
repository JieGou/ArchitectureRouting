using System ;
using System.Linq ;

namespace Arent3d.Architecture.Routing.AppBase.Extensions
{
  public static class DelegateExtension
  {
    public static Func<T, bool> AndAlso<T>(this Func<T, bool> predicateFirst, Func<T, bool> predicateSecond)
    {
      return arg => predicateFirst(arg) && predicateSecond(arg);
    }

    public static Func<T, bool> OrElse<T>(this Func<T, bool> predicateFirst, Func<T, bool> predicateSecond)
    {
      return arg => predicateFirst(arg) || predicateSecond(arg);
    }

    public static Func<T, bool> And<T>(params Func<T, bool>[] predicates)
    {
      return t => predicates.All(predicate => predicate(t));
    }

    public static Func<T, bool> Or<T>(params Func<T, bool>[] predicates)
    {
      return t => predicates.Any(predicate => predicate(t));
    }
  }
}