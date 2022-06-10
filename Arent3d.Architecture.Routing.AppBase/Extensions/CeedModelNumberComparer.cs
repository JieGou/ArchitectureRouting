using System ;
using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.AppBase.Extensions
{
  public class CeedModelNumberComparer : IComparer<string>
  {
    public int Compare( string x, string y )
    {
      var orderRule = CreateOrderCeedModelNumberRule() ;
      
      var xStrings = x.Split('_');
      var yStrings = y.Split('_');

      var minLengthOfString = xStrings.Length < yStrings.Length ? xStrings.Length : yStrings.Length;

      for(var i = 0; i < minLengthOfString; i++)
      {
        switch ( i ) {
          case 2 :
          {
            var lastXStringIndex = Array.IndexOf(orderRule, xStrings[i]);
            var lastYStringIndex = Array.IndexOf(orderRule, yStrings[i]);

            return lastXStringIndex.CompareTo(lastYStringIndex);
          }
          case 1 :
            int.TryParse(xStrings[i],out var xNumber);
            int.TryParse(yStrings[i],out var yNumber);

            return xNumber.CompareTo(yNumber);
        }
        
        if (xStrings[i] != yStrings[i])
        {
          return string.Compare(xStrings[i], yStrings[i], StringComparison.Ordinal);
        }
      }
      return 0;
    }

    private string[] CreateOrderCeedModelNumberRule()
    {
      return new[] { "P", "K", "F", "E", "G", "M", "U" } ;
    }
  }
}