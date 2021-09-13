using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture.Routing.Extensions
{
  public static class DictionaryExtensions
  {

    public static TValue GetOrDefault<Tkey, TValue>( this Dictionary<Tkey, TValue> keyValues, Tkey key, TValue defaultValue )
    {
      return keyValues.ContainsKey(key) ? keyValues[key] ?? defaultValue : defaultValue;
    }
  }
}
