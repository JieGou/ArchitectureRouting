using System.Collections.Generic;
using System.Linq;
using Arent3d.Utility;

namespace Arent3d.Architecture.Routing.AppBase.Extensions
{
    public class CeedModelNumberComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var orderRule = CreateOrderCeedModelNumberRule();

            if (x.Any(x=>x == '_') && y.Any(y=>y=='_'))
            {
                var xStrings = x.Split('_');
                var yStrings = y.Split('_');

                if (xStrings.Length > 2 && yStrings.Length > 2)
                {
                    var lastXStringIndex = 0;
                    var lastYStringIndex = 0;
                    var lastXCharacter = xStrings.LastOrDefault();
                    var lastYCharacter = yStrings.LastOrDefault();
                    if (orderRule.Any(x => x == lastXCharacter) && orderRule.Any(y => y == lastYCharacter))
                    {
                        lastXStringIndex = orderRule.IndexOf(lastXCharacter);
                        lastYStringIndex = orderRule.IndexOf(lastYCharacter);
                        return lastXStringIndex.CompareTo(lastYStringIndex);
                    }
                    else
                    {
                        return x.CompareTo(y);
                    }
                        
                }
                else
                {
                    return x.CompareTo(y);
                }
                
            }
            else
            {
                return x.CompareTo(y);
            }
        }
        
        private string[] CreateOrderCeedModelNumberRule()
        {
            return new string[]{"P","K","F","E","G","M","U"};
        }
    }
}