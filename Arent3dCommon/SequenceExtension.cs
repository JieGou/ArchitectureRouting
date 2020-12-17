using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d
{
  public static class SequenceExtension
  {
    public static IReadOnlyCollection<T> EnumerateAll<T>( this IEnumerable<T> enu )
    {
      if ( enu is IReadOnlyCollection<T> col ) return col;

      return enu.ToList();
    }

    public static void ForEach<T>( this IEnumerable<T> enu, Action<T> action )
    {
      foreach ( var t in enu ) action( t );
    }

    public static void ForEach<T>( this IEnumerable<T> enu, Action<T, int> action )
    {
      int i = 0;
      foreach ( var t in enu ) {
        action( t, i );
        ++i;
      }
    }

    public static T MinItemOrDefault<T>( this IEnumerable<T> enu, Func<T, double> evaluator )
    {
      T minItem = default;
      var maxEvaluation = double.PositiveInfinity;
      foreach ( var item in enu ) {
        var eval = evaluator( item );
        if ( maxEvaluation <= eval ) continue;

        minItem = item;
        maxEvaluation = eval;
      }
      return minItem;
    }

    public static bool IsUnique<T>( this IEnumerable<T> enu )
    {
      var isUnique = false;

      foreach ( var _ in enu ) {
        if ( isUnique ) return false;
        isUnique = true;
      }

      return isUnique;
    }
    public static T UniqueOrDefault<T>( this IEnumerable<T> enu )
    {
      return enu.UniqueOrDefault( default );
    }
    public static T UniqueOrDefault<T>( this IEnumerable<T> enu, T defaultValue )
    {
      bool isUnique = false;
      T uniqueValue = defaultValue;

      foreach ( var e in enu ) {
        if ( isUnique ) return defaultValue;
        isUnique = true;
        uniqueValue = e;
      }

      return uniqueValue;
    }

    public static SortedList<TKey, TValue> ToSortedList<TKey, TValue>( this IEnumerable<TValue> enu, Converter<TValue, TKey> keySelector )
    {
      var list = new SortedList<TKey, TValue>();
      foreach ( var value in enu ) {
        list.Add( keySelector( value ), value );
      }

      return list;
    }

    public static void Fill<T>( this T[] array, T value )
    {
      for ( int i = 0 ; i < array.Length ; i++ ) {
        array[i] = value;
      }
    }

    public static T[] SubArray<T>( this T[] array, int startIndex )
    {
      return array.SubArray( startIndex, array.Length - startIndex );
    }

    public static T[] SubArray<T>( this T[] array, int startIndex, int length )
    {
      if ( null == array ) throw new ArgumentNullException( nameof( Array ) );
      if ( startIndex < 0 ) throw new ArgumentOutOfRangeException( nameof( startIndex ) );
      if ( array.Length < startIndex + length ) throw new ArgumentOutOfRangeException( nameof( startIndex ) + " + " + nameof( length ) );

      var result = new T[length];
      Array.Copy( array, startIndex, result, 0, length );
      return result;
    }

    public static T[] Concat<T>( this T[] array1, params T[] array2 )
    {
      if ( null == array1 ) return array2;
      if ( null == array2 ) return array1;

      var result = new T[array1.Length + array2.Length];
      Array.Copy( array1, result, array1.Length );
      Array.Copy( array2, 0, result, array1.Length, array2.Length );
      return result;
    }

    public static T[] ToArray<T>( this IReadOnlyCollection<T> col )
    {
      var buffer = new T[col.Count];

      if ( col is ICollection<T> c ) {
        c.CopyTo( buffer, 0 );
      }
      else {
        var index = 0;
        foreach ( var value in col ) {
          buffer[index++] = value;
        }
      }

      return buffer;
    }

    public static TTo[] ToArray<TFrom, TTo>( this IReadOnlyCollection<TFrom> col, Converter<TFrom, TTo> converter )
    {
      var buffer = new TTo[col.Count];

      var index = 0;
      foreach ( var value in col ) {
        buffer[index++] = converter( value );
      }

      return buffer;
    }
  }
}
